using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Orders.Application.Orders;
using Orders.Application.Orders.Contracts;
using Orders.Application.Orders.Persistence;
using Orders.Application.Orders.Validators;
using Orders.Domain.Common;
using Orders.Domain.Orders;

namespace Orders.UnitTests.Application;

public sealed class OrderServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeOrderRepository _repository = new();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _service = new OrderService(
            _repository,
            new CreateOrderRequestValidator(),
            new UpdateOrderRequestValidator(),
            new ListOrdersQueryValidator(),
            _time,
            NullLogger<OrderService>.Instance);
    }

    private static UpdateOrderRequest Update(string status = "Open", decimal amount = 200m) =>
        new("Maria Souza", "Reposição de gôndola", amount, status);

    private async Task<OrderResponse> SeedOrderAsync(string status = "Open")
    {
        var created = (await _service.CreateAsync(
            new CreateOrderRequest("Maria Souza", "Reposição de gôndola", 100m), default)).Value;

        return status == "Open"
            ? created
            : (await _service.UpdateAsync(created.Id, Update(status), null, default)).Value;
    }

    // ---- Create ----

    [Fact]
    public async Task Create_Valid_PersistsOpenOrderWithServerFields()
    {
        var result = await _service.CreateAsync(new CreateOrderRequest(" Maria Souza ", "Reposição", 99.9m), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new
        {
            Number = 10001L,
            CustomerName = "Maria Souza",
            Status = OrderStatus.Open,
            CreatedAt = Now,
            Version = 1L,
        });
        (await _repository.GetAsync(result.Value.Id, default)).Should().NotBeNull();
    }

    [Fact]
    public async Task Create_Invalid_ReturnsValidationErrorGroupedByField()
    {
        var result = await _service.CreateAsync(new CreateOrderRequest("", "Reposição", 0), default);

        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("request.validation");
        result.Error.Fields.Should().ContainKeys("CustomerName", "TotalAmount").And.HaveCount(2);
        _repository.NextNumber().Should().Be(10001, "pedido inválido não consome número");
    }

    // ---- Get ----

    [Fact]
    public async Task Get_Missing_ReturnsNotFound()
    {
        (await _service.GetAsync(Guid.NewGuid(), default)).Error.Should().Be(OrderErrors.NotFound);
    }

    // ---- Update ----

    [Fact]
    public async Task Update_WithMatchingVersion_AppliesAndBumpsVersion()
    {
        var order = await SeedOrderAsync();
        _time.Advance(TimeSpan.FromMinutes(5));

        var result = await _service.UpdateAsync(order.Id, Update("Paid", 250m), expectedVersion: 1, default);

        result.Value.Should().BeEquivalentTo(new
        {
            Status = OrderStatus.Paid,
            TotalAmount = 250m,
            Version = 2L,
            UpdatedAt = Now.AddMinutes(5),
        });
    }

    [Fact]
    public async Task Update_WithStaleVersion_ReturnsPreconditionFailedAndKeepsOrder()
    {
        var order = await SeedOrderAsync();

        var result = await _service.UpdateAsync(order.Id, Update(), expectedVersion: 7, default);

        result.Error.Should().Be(OrderErrors.VersionMismatch);
        result.Error!.Type.Should().Be(ErrorType.PreconditionFailed);
        (await _repository.GetAsync(order.Id, default))!.Version.Should().Be(1);
    }

    [Theory]
    [InlineData("Paid")]
    [InlineData("Cancelled")]
    public async Task Update_FinalOrder_ReturnsImmutableState(string finalStatus)
    {
        var order = await SeedOrderAsync(finalStatus);

        var result = await _service.UpdateAsync(order.Id, Update("Open"), null, default);

        result.Error!.Code.Should().Be("order.immutable_state");
    }

    [Fact]
    public async Task Update_Missing_ReturnsNotFound()
    {
        (await _service.UpdateAsync(Guid.NewGuid(), Update(), null, default)).Error.Should().Be(OrderErrors.NotFound);
    }

    [Fact]
    public async Task Update_InvalidBody_IsRejectedBeforeLookingUpOrder()
    {
        var result = await _service.UpdateAsync(Guid.NewGuid(), Update(status: "Shipped"), null, default);

        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error.Fields.Should().ContainKey("Status");
    }

    // T10: alguém paga o pedido entre a nossa leitura e a nossa escrita (sem If-Match).
    [Fact]
    public async Task Update_ConcurrentPayment_RetriesAndRespectsStateMachine()
    {
        var order = await SeedOrderAsync();
        PayOnFirstWrite();

        var result = await _service.UpdateAsync(order.Id, Update("Cancelled"), expectedVersion: null, default);

        result.Error!.Code.Should().Be("order.immutable_state", "na releitura o pedido já está pago");
        (await _repository.GetAsync(order.Id, default))!.Status.Should().Be(OrderStatus.Paid);
    }

    // Mesma corrida, mas o cliente enviou If-Match: deve receber 412 e não sobrescrever.
    [Fact]
    public async Task Update_ConcurrentPaymentWithIfMatch_ReturnsVersionMismatch()
    {
        var order = await SeedOrderAsync();
        PayOnFirstWrite();

        var result = await _service.UpdateAsync(order.Id, Update("Cancelled"), expectedVersion: 1, default);

        result.Error.Should().Be(OrderErrors.VersionMismatch);
    }

    // Corrida sem conflito de regra: a edição concorrente só mudou o valor; nossa escrita é reaplicada.
    [Fact]
    public async Task Update_ConcurrentEditWithoutIfMatch_ReappliesOnLatestVersion()
    {
        var order = await SeedOrderAsync();
        var fired = false;
        _repository.BeforeWrite = current =>
        {
            if (!fired)
            {
                fired = true;
                _repository.Replace(current.Update("Outro", "Edição concorrente", 1m, OrderStatus.Open, Now).Value);
            }

            return Task.CompletedTask;
        };

        var result = await _service.UpdateAsync(order.Id, Update(amount: 300m), null, default);

        result.Value.Version.Should().Be(3);
        result.Value.TotalAmount.Should().Be(300m);
        _repository.WriteAttempts.Should().Be(2);
    }

    [Fact]
    public async Task Update_PersistentContention_GivesUpWithConflict()
    {
        var order = await SeedOrderAsync();
        _repository.BeforeWrite = current =>
        {
            _repository.Replace(current.Update("Outro", "Edição concorrente", 1m, OrderStatus.Open, Now).Value);
            return Task.CompletedTask;
        };

        var result = await _service.UpdateAsync(order.Id, Update(), null, default);

        result.Error.Should().Be(OrderErrors.ConcurrencyConflict);
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        _repository.WriteAttempts.Should().Be(OrderService.MaxWriteAttempts);
    }

    // ---- Delete ----

    [Theory]
    [InlineData("Open")]
    [InlineData("Cancelled")]
    public async Task Delete_OpenOrCancelled_RemovesOrder(string status)
    {
        var order = await SeedOrderAsync(status);

        (await _service.DeleteAsync(order.Id, order.Version, default)).IsSuccess.Should().BeTrue();
        (await _service.GetAsync(order.Id, default)).Error.Should().Be(OrderErrors.NotFound);
    }

    [Fact]
    public async Task Delete_Paid_ReturnsDeleteNotAllowed()
    {
        var order = await SeedOrderAsync("Paid");

        (await _service.DeleteAsync(order.Id, null, default)).Error.Should().Be(OrderErrors.DeleteNotAllowed);
    }

    [Fact]
    public async Task Delete_Twice_SecondReturnsNotFound()
    {
        var order = await SeedOrderAsync();
        await _service.DeleteAsync(order.Id, null, default);

        (await _service.DeleteAsync(order.Id, null, default)).Error.Should().Be(OrderErrors.NotFound);
    }

    [Fact]
    public async Task Delete_StaleVersion_ReturnsVersionMismatch()
    {
        var order = await SeedOrderAsync();

        (await _service.DeleteAsync(order.Id, expectedVersion: 2, default)).Error.Should().Be(OrderErrors.VersionMismatch);
    }

    // Pedido pago por outra pessoa entre a leitura e a exclusão: não pode ser excluído.
    [Fact]
    public async Task Delete_ConcurrentPayment_ReturnsDeleteNotAllowed()
    {
        var order = await SeedOrderAsync();
        PayOnFirstWrite();

        (await _service.DeleteAsync(order.Id, null, default)).Error.Should().Be(OrderErrors.DeleteNotAllowed);
        (await _repository.GetAsync(order.Id, default)).Should().NotBeNull();
    }

    // ---- List ----

    [Fact]
    public async Task List_Defaults_MapToCreatedAtDescending()
    {
        var result = await _service.ListAsync(new ListOrdersQuery(), default);

        result.IsSuccess.Should().BeTrue();
        _repository.LastCriteria.Should().Be(new OrderListCriteria(null, null, 1, 20, OrderSortField.CreatedAt, true));
    }

    [Fact]
    public async Task List_Filters_AreParsedAndTrimmed()
    {
        await _service.ListAsync(
            new ListOrdersQuery { Search = "  joão ", Status = "paid", Page = 2, PageSize = 50, SortBy = "totalAmount", SortDir = "asc" },
            default);

        _repository.LastCriteria.Should().Be(
            new OrderListCriteria("joão", OrderStatus.Paid, 2, 50, OrderSortField.TotalAmount, false));
    }

    [Fact]
    public async Task List_InvalidQuery_ReturnsValidationAndSkipsRepository()
    {
        var result = await _service.ListAsync(new ListOrdersQuery { PageSize = 500 }, default);

        result.Error!.Fields.Should().ContainKey("PageSize");
        _repository.LastCriteria.Should().BeNull();
    }

    [Fact]
    public async Task List_ComputesTotalPages()
    {
        for (var i = 0; i < 3; i++)
        {
            await SeedOrderAsync();
        }

        var page = (await _service.ListAsync(new ListOrdersQuery { PageSize = 2 }, default)).Value;

        page.TotalItems.Should().Be(3);
        page.TotalPages.Should().Be(2);
    }

    private void PayOnFirstWrite()
    {
        var fired = false;
        _repository.BeforeWrite = current =>
        {
            if (!fired)
            {
                fired = true;
                _repository.Replace(current.Update(
                    current.CustomerName, current.Description, current.TotalAmount, OrderStatus.Paid, Now).Value);
            }

            return Task.CompletedTask;
        };
    }
}
