using Orders.Application.Orders;
using Orders.Domain.Orders;
using Orders.Infrastructure.Orders;

namespace Orders.UnitTests.Infrastructure;

public sealed class InMemoryOrderRepositoryTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryOrderRepository _repository = new();

    private async Task<Order> AddAsync(
        string customer = "Maria Souza", string description = "Reposição de gôndola",
        decimal amount = 100m, int minutes = 0, OrderStatus status = OrderStatus.Open)
    {
        var order = Order.Create(_repository.NextNumber(), customer, description, amount, T0.AddMinutes(minutes));
        if (status != OrderStatus.Open)
        {
            order = order.Update(customer, description, amount, status, T0.AddMinutes(minutes + 1)).Value;
        }

        await _repository.AddAsync(order, default);
        return order;
    }

    private Task<PagedResult<Order>> ListAsync(
        string? search = null, OrderStatus? status = null, int page = 1, int pageSize = 20,
        OrderSortField sortBy = OrderSortField.CreatedAt, bool descending = true) =>
        _repository.ListAsync(new OrderListCriteria(search, status, page, pageSize, sortBy, descending), default);

    [Fact]
    public void NextNumber_StartsAt10001AndIncrements()
    {
        _repository.NextNumber().Should().Be(10001);
        _repository.NextNumber().Should().Be(10002);
    }

    [Fact]
    public async Task Add_ThenGet_ReturnsSameOrder()
    {
        var order = await AddAsync();

        (await _repository.GetAsync(order.Id, default)).Should().BeSameAs(order);
    }

    [Fact]
    public async Task Add_DuplicateId_Throws()
    {
        var order = await AddAsync();

        var act = () => _repository.AddAsync(order, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TryUpdate_FromCurrentVersion_Succeeds()
    {
        var v1 = await AddAsync();
        var v2 = v1.Update("Outro", "Outra descrição", 5m, OrderStatus.Open, T0).Value;

        (await _repository.TryUpdateAsync(v1, v2, default)).Should().BeTrue();
        (await _repository.GetAsync(v1.Id, default)).Should().BeSameAs(v2);
    }

    [Fact]
    public async Task TryUpdate_FromStaleVersion_FailsAndKeepsStored()
    {
        var v1 = await AddAsync();
        var v2 = v1.Update("Outro", "Outra descrição", 5m, OrderStatus.Open, T0).Value;
        await _repository.TryUpdateAsync(v1, v2, default);

        var lostUpdate = v1.Update("Atrasado", "Escrita atrasada", 9m, OrderStatus.Open, T0).Value;

        (await _repository.TryUpdateAsync(v1, lostUpdate, default)).Should().BeFalse();
        (await _repository.GetAsync(v1.Id, default)).Should().BeSameAs(v2);
    }

    [Fact]
    public async Task TryRemove_OnlyWhenStoredIsCurrent()
    {
        var v1 = await AddAsync();
        var v2 = v1.Update("Outro", "Outra descrição", 5m, OrderStatus.Open, T0).Value;
        await _repository.TryUpdateAsync(v1, v2, default);

        (await _repository.TryRemoveAsync(v1, default)).Should().BeFalse("v1 já foi substituído");
        (await _repository.TryRemoveAsync(v2, default)).Should().BeTrue();
        (await _repository.GetAsync(v1.Id, default)).Should().BeNull();
    }

    [Fact]
    public async Task List_DefaultsToNewestFirst()
    {
        var older = await AddAsync(minutes: 0);
        var newer = await AddAsync(minutes: 10);

        var result = await ListAsync();

        result.Items.Should().Equal(newer, older);
        result.TotalItems.Should().Be(2);
    }

    [Theory]
    [InlineData("joao conceicao")]
    [InlineData("JOÃO")]
    [InlineData("  conceição ")]
    [InlineData("10001")]
    [InlineData("gondola")]
    public async Task List_Search_IgnoresCaseAccentsAndMatchesNumber(string term)
    {
        await AddAsync(customer: "João Conceição", description: "Reposição de gôndola");
        await AddAsync(customer: "Maria Souza", description: "Pedido mensal");

        var result = await ListAsync(search: term);

        result.Items.Should().ContainSingle().Which.CustomerName.Should().Be("João Conceição");
    }

    [Fact]
    public async Task List_SearchAfterUpdate_UsesNewText()
    {
        var v1 = await AddAsync(customer: "Maria Souza");
        await _repository.TryUpdateAsync(v1, v1.Update("Ana Lima", "Pedido mensal", 5m, OrderStatus.Open, T0).Value, default);

        (await ListAsync(search: "maria")).TotalItems.Should().Be(0);
        (await ListAsync(search: "ana")).TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task List_StatusAndSearch_AreCombined()
    {
        await AddAsync(customer: "Maria Paga", status: OrderStatus.Paid);
        await AddAsync(customer: "Maria Aberta");
        await AddAsync(customer: "José Pago", status: OrderStatus.Paid);

        var result = await ListAsync(search: "maria", status: OrderStatus.Paid);

        result.Items.Should().ContainSingle().Which.CustomerName.Should().Be("Maria Paga");
    }

    [Fact]
    public async Task List_Paging_ReturnsSliceAndTotal()
    {
        for (var i = 0; i < 5; i++)
        {
            await AddAsync(minutes: i);
        }

        var page2 = await ListAsync(page: 2, pageSize: 2, sortBy: OrderSortField.Number, descending: false);

        page2.Items.Select(o => o.Number).Should().Equal(10003, 10004);
        page2.TotalItems.Should().Be(5);
    }

    [Fact]
    public async Task List_PageBeyondTotal_IsEmptyWithTotals()
    {
        await AddAsync();

        var result = await ListAsync(page: 50);

        result.Items.Should().BeEmpty();
        result.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task List_TiesAreBrokenByNumber()
    {
        var a = await AddAsync(amount: 50m);
        var b = await AddAsync(amount: 50m);
        var c = await AddAsync(amount: 10m);

        (await ListAsync(sortBy: OrderSortField.TotalAmount, descending: false)).Items.Should().Equal(c, a, b);
        (await ListAsync(sortBy: OrderSortField.TotalAmount, descending: true)).Items.Should().Equal(b, a, c);
    }

    [Fact]
    public async Task List_SortByCustomerName_UsesPortugueseCulture()
    {
        await AddAsync(customer: "Bruno");
        await AddAsync(customer: "álvaro");
        await AddAsync(customer: "Carla");

        var result = await ListAsync(sortBy: OrderSortField.CustomerName, descending: false);

        result.Items.Select(o => o.CustomerName).Should().Equal("álvaro", "Bruno", "Carla");
    }
}
