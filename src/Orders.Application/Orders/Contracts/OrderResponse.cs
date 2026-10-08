using Orders.Domain.Entities;
using Orders.Domain.Enums;

namespace Orders.Application.Orders.Contracts;

public sealed record OrderResponse(
    Guid Id,
    long Number,
    string CustomerName,
    string Description,
    decimal TotalAmount,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long Version)
{
    public static OrderResponse From(Order order) => new(
        order.Id,
        order.Number,
        order.CustomerName,
        order.Description,
        order.TotalAmount,
        order.Status,
        order.CreatedAt,
        order.UpdatedAt,
        order.Version);
}
