using System.Collections.Concurrent;
using System.Globalization;
using Orders.Application.Common;
using Orders.Application.Orders.Persistence;
using Orders.Domain.Entities;

namespace Orders.Infrastructure.Persistence;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    public const long FirstNumber = 10001;

    private static readonly StringComparer CustomerNameComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("pt-BR"), CompareOptions.IgnoreCase);

    private readonly ConcurrentDictionary<Guid, OrderRecord> _orders = new();
    private long _lastNumber = FirstNumber - 1;

    public int Count => _orders.Count;

    public long NextNumber() => Interlocked.Increment(ref _lastNumber);

    public Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_orders.TryGetValue(id, out var record) ? record.ToEntity() : null);

    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        if (!_orders.TryAdd(order.Id, OrderRecord.FromEntity(order)))
        {
            throw new InvalidOperationException($"Já existe um pedido com id {order.Id}.");
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryUpdateAsync(Order order, long expectedVersion, CancellationToken cancellationToken)
    {
        if (!_orders.TryGetValue(order.Id, out var stored) || stored.Version != expectedVersion)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_orders.TryUpdate(order.Id, OrderRecord.FromEntity(order), stored));
    }

    public Task<bool> TryRemoveAsync(Guid id, long expectedVersion, CancellationToken cancellationToken)
    {
        if (!_orders.TryGetValue(id, out var stored) || stored.Version != expectedVersion)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_orders.TryRemove(KeyValuePair.Create(id, stored)));
    }

    public Task<PagedResult<Order>> ListAsync(OrderListCriteria criteria, CancellationToken cancellationToken)
    {
        IEnumerable<OrderRecord> query = _orders.Values;

        if (criteria.Status is { } status)
        {
            query = query.Where(r => r.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var term = SearchNormalizer.Normalize(criteria.Search);
            query = query.Where(r => r.SearchKey.Contains(term, StringComparison.Ordinal));
        }

        var matches = query.ToList();
        var page = Sort(matches, criteria.SortBy, criteria.Descending)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(r => r.ToEntity())
            .ToList();

        return Task.FromResult(new PagedResult<Order>(page, matches.Count));
    }

    private static IOrderedEnumerable<OrderRecord> Sort(List<OrderRecord> records, OrderSortField field, bool descending)
    {
        var sorted = field switch
        {
            OrderSortField.Number => OrderBy(records, r => r.Number, descending),
            OrderSortField.CustomerName => descending
                ? records.OrderByDescending(r => r.CustomerName, CustomerNameComparer)
                : records.OrderBy(r => r.CustomerName, CustomerNameComparer),
            OrderSortField.TotalAmount => OrderBy(records, r => r.TotalAmount, descending),
            OrderSortField.Status => OrderBy(records, r => r.Status, descending),
            _ => OrderBy(records, r => r.CreatedAt, descending),
        };

        return descending ? sorted.ThenByDescending(r => r.Number) : sorted.ThenBy(r => r.Number);
    }

    private static IOrderedEnumerable<OrderRecord> OrderBy<TKey>(
        List<OrderRecord> records, Func<OrderRecord, TKey> key, bool descending) =>
        descending ? records.OrderByDescending(key) : records.OrderBy(key);
}
