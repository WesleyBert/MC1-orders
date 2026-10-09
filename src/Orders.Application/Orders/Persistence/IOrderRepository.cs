using Orders.Application.Common;
using Orders.Domain.Entities;
using Orders.Domain.Enums;

namespace Orders.Application.Orders.Persistence;

public interface IOrderRepository
{
    long NextNumber();

    Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Order order, CancellationToken cancellationToken);

    Task<bool> TryUpdateAsync(Order order, long expectedVersion, CancellationToken cancellationToken);

    Task<bool> TryRemoveAsync(Guid id, long expectedVersion, CancellationToken cancellationToken);

    Task<PagedResult<Order>> ListAsync(OrderListCriteria criteria, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<OrderStatus, int>> CountByStatusAsync(CancellationToken cancellationToken);
}
