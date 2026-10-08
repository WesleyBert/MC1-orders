using Orders.Application.Orders;
using Orders.Domain.Orders;

namespace Orders.UnitTests.Application;

/// <summary>
/// Repositório de teste com mesma semântica de compare-and-swap do real, mais um gancho
/// <see cref="BeforeWrite"/> para simular, de forma determinística, outro usuário gravando
/// entre a leitura e a escrita do serviço.
/// </summary>
internal sealed class FakeOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _orders = [];
    private long _lastNumber = 10000;

    public Func<Order, Task>? BeforeWrite { get; set; }

    public int WriteAttempts { get; private set; }

    public OrderListCriteria? LastCriteria { get; private set; }

    public long NextNumber() => ++_lastNumber;

    public Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_orders.GetValueOrDefault(id));

    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        _orders.Add(order.Id, order);
        return Task.CompletedTask;
    }

    /// <summary>Grava direto, como faria uma requisição concorrente.</summary>
    public void Replace(Order order) => _orders[order.Id] = order;

    public async Task<bool> TryUpdateAsync(Order current, Order updated, CancellationToken cancellationToken)
    {
        await SimulateConcurrentWriter(current);
        if (!_orders.TryGetValue(current.Id, out var stored) || !stored.Equals(current))
        {
            return false;
        }

        _orders[current.Id] = updated;
        return true;
    }

    public async Task<bool> TryRemoveAsync(Order current, CancellationToken cancellationToken)
    {
        await SimulateConcurrentWriter(current);
        return _orders.TryGetValue(current.Id, out var stored) && stored.Equals(current) && _orders.Remove(current.Id);
    }

    public Task<PagedResult<Order>> ListAsync(OrderListCriteria criteria, CancellationToken cancellationToken)
    {
        LastCriteria = criteria;
        return Task.FromResult(new PagedResult<Order>([.. _orders.Values], _orders.Count));
    }

    private async Task SimulateConcurrentWriter(Order current)
    {
        WriteAttempts++;
        if (BeforeWrite is not null)
        {
            await BeforeWrite(current);
        }
    }
}
