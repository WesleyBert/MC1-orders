using Orders.Application.Common;
using Orders.Domain.Orders;

namespace Orders.Application.Orders.Persistence;

/// <summary>
/// Persistência de pedidos. As escritas são compare-and-swap: só têm efeito se o pedido armazenado
/// ainda for exatamente o <c>current</c> lido antes, o que impede lost updates sem lock global.
/// Assíncrono mesmo em memória para permitir troca por banco sem mudar a aplicação.
/// </summary>
public interface IOrderRepository
{
    /// <summary>Próximo número sequencial de pedido (thread-safe, nunca reutilizado).</summary>
    long NextNumber();

    Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Order order, CancellationToken cancellationToken);

    /// <returns><c>false</c> se o pedido foi alterado ou removido desde que <paramref name="current"/> foi lido.</returns>
    Task<bool> TryUpdateAsync(Order current, Order updated, CancellationToken cancellationToken);

    /// <returns><c>false</c> se o pedido foi alterado ou removido desde que <paramref name="current"/> foi lido.</returns>
    Task<bool> TryRemoveAsync(Order current, CancellationToken cancellationToken);

    Task<PagedResult<Order>> ListAsync(OrderListCriteria criteria, CancellationToken cancellationToken);
}
