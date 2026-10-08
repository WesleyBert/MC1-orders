using Orders.Application.Common;
using Orders.Application.Orders.Persistence;
using Orders.Domain.Entities;

namespace Orders.UnitTests.Application;

internal sealed class FakeOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _orders = [];
    private long _lastNumber = 10000;

    public Func<Guid, Task>? BeforeWrite { get; set; }

    public int WriteAttempts { get; private set; }

    public OrderListCriteria? LastCriteria { get; private set; }

    public long NextNumber() => ++_lastNumber;

    public Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_orders.TryGetValue(id, out var order) ? Copy(order) : null);

    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        _orders.Add(order.Id, Copy(order));
        return Task.CompletedTask;
    }

    public void ChangeStored(Guid id, Action<Order> change)
    {
        var order = Copy(_orders[id]);
        change(order);
        _orders[id] = order;
    }

    public async Task<bool> TryUpdateAsync(Order order, long expectedVersion, CancellationToken cancellationToken)
    {
        await SimulateConcurrentWriter(order.Id);
        if (!_orders.TryGetValue(order.Id, out var stored) || stored.Version != expectedVersion)
        {
            return false;
        }

        _orders[order.Id] = Copy(order);
        return true;
    }

    public async Task<bool> TryRemoveAsync(Guid id, long expectedVersion, CancellationToken cancellationToken)
    {
        await SimulateConcurrentWriter(id);
        return _orders.TryGetValue(id, out var stored) && stored.Version == expectedVersion && _orders.Remove(id);
    }

    public Task<PagedResult<Order>> ListAsync(OrderListCriteria criteria, CancellationToken cancellationToken)
    {
        LastCriteria = criteria;
        return Task.FromResult(new PagedResult<Order>([.. _orders.Values.Select(Copy)], _orders.Count));
    }

    private static Order Copy(Order o) => Order.Restore(
        o.Id, o.Number, o.CustomerName, o.Description, o.TotalAmount, o.Status, o.CreatedAt, o.UpdatedAt, o.Version);

    private async Task SimulateConcurrentWriter(Guid id)
    {
        WriteAttempts++;
        if (BeforeWrite is not null)
        {
            await BeforeWrite(id);
        }
    }
}
