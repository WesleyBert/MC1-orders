using Orders.Domain.Entities;
using Orders.Domain.Enums;

namespace Orders.Infrastructure.Persistence;

internal sealed record OrderRecord(
    Guid Id,
    long Number,
    string CustomerName,
    string Description,
    decimal TotalAmount,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long Version,
    string SearchKey)
{
    public static OrderRecord FromEntity(Order order) => new(
        order.Id,
        order.Number,
        order.CustomerName,
        order.Description,
        order.TotalAmount,
        order.Status,
        order.CreatedAt,
        order.UpdatedAt,
        order.Version,
        SearchNormalizer.Normalize($"{order.Number} {order.CustomerName} {order.Description}"));

    public Order ToEntity() =>
        Order.Restore(Id, Number, CustomerName, Description, TotalAmount, Status, CreatedAt, UpdatedAt, Version);
}
