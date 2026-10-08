using Orders.Application.Common;
using Orders.Application.Orders.Persistence;
using Orders.Domain.Entities;
using Orders.Domain.Enums;
using Orders.Infrastructure.Persistence;

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
            order.Update(customer, description, amount, status, T0.AddMinutes(minutes + 1));
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
    public async Task Get_ReturnsEquivalentCopyNotTheStoredInstance()
    {
        var order = await AddAsync();

        var first = await _repository.GetAsync(order.Id, default);
        var second = await _repository.GetAsync(order.Id, default);

        first.Should().BeEquivalentTo(order);
        first.Should().NotBeSameAs(second);
    }

    [Fact]
    public async Task ChangingLoadedEntity_DoesNotAffectStoreUntilSaved()
    {
        var order = await AddAsync();
        var loaded = (await _repository.GetAsync(order.Id, default))!;

        loaded.Update("Outro", "Outra descrição", 5m, OrderStatus.Paid, T0);

        (await _repository.GetAsync(order.Id, default))!.Status.Should().Be(OrderStatus.Open);
    }

    [Fact]
    public async Task Add_DuplicateId_Throws()
    {
        var order = await AddAsync();

        var act = () => _repository.AddAsync(order, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TryUpdate_WithExpectedVersion_Succeeds()
    {
        var order = await AddAsync();
        var loaded = (await _repository.GetAsync(order.Id, default))!;
        loaded.Update("Outro", "Outra descrição", 5m, OrderStatus.Open, T0);

        (await _repository.TryUpdateAsync(loaded, expectedVersion: 1, default)).Should().BeTrue();
        (await _repository.GetAsync(order.Id, default)).Should().BeEquivalentTo(new { CustomerName = "Outro", Version = 2L });
    }

    [Fact]
    public async Task TryUpdate_WithStaleVersion_FailsAndKeepsStored()
    {
        var order = await AddAsync();
        var first = (await _repository.GetAsync(order.Id, default))!;
        var second = (await _repository.GetAsync(order.Id, default))!;

        first.Update("Primeiro", "Primeira escrita", 5m, OrderStatus.Open, T0);
        await _repository.TryUpdateAsync(first, expectedVersion: 1, default);

        second.Update("Segundo", "Escrita atrasada", 9m, OrderStatus.Open, T0);

        (await _repository.TryUpdateAsync(second, expectedVersion: 1, default)).Should().BeFalse();
        (await _repository.GetAsync(order.Id, default))!.CustomerName.Should().Be("Primeiro");
    }

    [Fact]
    public async Task TryRemove_OnlyWithCurrentVersion()
    {
        var order = await AddAsync();
        var loaded = (await _repository.GetAsync(order.Id, default))!;
        loaded.Update("Outro", "Outra descrição", 5m, OrderStatus.Open, T0);
        await _repository.TryUpdateAsync(loaded, expectedVersion: 1, default);

        (await _repository.TryRemoveAsync(order.Id, expectedVersion: 1, default)).Should().BeFalse();
        (await _repository.TryRemoveAsync(order.Id, expectedVersion: 2, default)).Should().BeTrue();
        (await _repository.GetAsync(order.Id, default)).Should().BeNull();
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
        var order = await AddAsync(customer: "Maria Souza");
        var loaded = (await _repository.GetAsync(order.Id, default))!;
        loaded.Update("Ana Lima", "Pedido mensal", 5m, OrderStatus.Open, T0);
        await _repository.TryUpdateAsync(loaded, expectedVersion: 1, default);

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
