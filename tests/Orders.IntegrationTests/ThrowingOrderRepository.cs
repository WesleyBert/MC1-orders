using Orders.Application.Common;
using Orders.Application.Orders.Persistence;
using Orders.Domain.Entities;

namespace Orders.IntegrationTests;

internal sealed class ThrowingOrderRepository : IOrderRepository
{
    public const string SecretMessage = "connection string secreta";

    public long NextNumber() => throw new InvalidOperationException(SecretMessage);

    public Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(SecretMessage);

    public Task AddAsync(Order order, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(SecretMessage);

    public Task<bool> TryUpdateAsync(Order order, long expectedVersion, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(SecretMessage);

    public Task<bool> TryRemoveAsync(Guid id, long expectedVersion, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(SecretMessage);

    public Task<PagedResult<Order>> ListAsync(OrderListCriteria criteria, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(SecretMessage);
}
