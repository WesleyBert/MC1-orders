using System.Collections.Concurrent;
using System.Globalization;
using Orders.Application.Orders;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Orders;

/// <summary>
/// Repositório em memória thread-safe, sem lock global:
/// <list type="bullet">
/// <item>Pedidos são imutáveis; cada escrita troca a instância inteira.</item>
/// <item>Atualização e exclusão são compare-and-swap: só têm efeito se o valor armazenado ainda for o lido.</item>
/// <item>Leituras nunca bloqueiam e nunca enxergam um pedido pela metade.</item>
/// <item>A chave de busca é calculada na escrita, não a cada listagem.</item>
/// </list>
/// </summary>
public sealed class InMemoryOrderRepository : IOrderRepository
{
    /// <summary>Primeiro número atribuído será 10001.</summary>
    public const long FirstNumber = 10001;

    private static readonly StringComparer CustomerNameComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("pt-BR"), CompareOptions.IgnoreCase);

    private readonly ConcurrentDictionary<Guid, Entry> _orders = new();
    private long _lastNumber = FirstNumber - 1;

    public int Count => _orders.Count;

    public long NextNumber() => Interlocked.Increment(ref _lastNumber);

    public Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_orders.TryGetValue(id, out var entry) ? entry.Order : null);

    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        if (!_orders.TryAdd(order.Id, Entry.For(order)))
        {
            throw new InvalidOperationException($"Já existe um pedido com id {order.Id}.");
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryUpdateAsync(Order current, Order updated, CancellationToken cancellationToken)
    {
        if (current.Id != updated.Id)
        {
            throw new ArgumentException("O id do pedido não pode mudar.", nameof(updated));
        }

        // Atômico: troca somente se o valor armazenado ainda for igual ao lido.
        return Task.FromResult(_orders.TryUpdate(current.Id, Entry.For(updated), Entry.For(current)));
    }

    public Task<bool> TryRemoveAsync(Order current, CancellationToken cancellationToken) =>
        Task.FromResult(_orders.TryRemove(KeyValuePair.Create(current.Id, Entry.For(current))));

    public Task<PagedResult<Order>> ListAsync(OrderListCriteria criteria, CancellationToken cancellationToken)
    {
        // Values devolve uma cópia consistente no instante da chamada: total e página
        // são calculados sobre o mesmo conjunto, mesmo com escritas em andamento.
        IEnumerable<Entry> query = _orders.Values;

        if (criteria.Status is { } status)
        {
            query = query.Where(e => e.Order.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var term = SearchNormalizer.Normalize(criteria.Search);
            query = query.Where(e => e.SearchKey.Contains(term, StringComparison.Ordinal));
        }

        var matches = query.Select(e => e.Order).ToList();
        var page = Sort(matches, criteria.SortBy, criteria.Descending)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<Order>(page, matches.Count));
    }

    // Desempate por número garante ordem estável entre requisições (paginação consistente).
    private static IOrderedEnumerable<Order> Sort(List<Order> orders, OrderSortField field, bool descending)
    {
        var sorted = field switch
        {
            OrderSortField.Number => OrderBy(orders, o => o.Number, descending),
            OrderSortField.CustomerName => descending
                ? orders.OrderByDescending(o => o.CustomerName, CustomerNameComparer)
                : orders.OrderBy(o => o.CustomerName, CustomerNameComparer),
            OrderSortField.TotalAmount => OrderBy(orders, o => o.TotalAmount, descending),
            OrderSortField.Status => OrderBy(orders, o => o.Status, descending),
            _ => OrderBy(orders, o => o.CreatedAt, descending),
        };

        return descending ? sorted.ThenByDescending(o => o.Number) : sorted.ThenBy(o => o.Number);
    }

    private static IOrderedEnumerable<Order> OrderBy<TKey>(List<Order> orders, Func<Order, TKey> key, bool descending) =>
        descending ? orders.OrderByDescending(key) : orders.OrderBy(key);

    /// <summary>Pedido + chave de busca normalizada (número, cliente e descrição).</summary>
    private sealed record Entry(Order Order, string SearchKey)
    {
        public static Entry For(Order order) => new(
            order,
            SearchNormalizer.Normalize($"{order.Number} {order.CustomerName} {order.Description}"));
    }
}
