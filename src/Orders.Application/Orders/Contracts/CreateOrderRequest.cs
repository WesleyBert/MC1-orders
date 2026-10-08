namespace Orders.Application.Orders.Contracts;

public sealed record CreateOrderRequest(string? CustomerName, string? Description, decimal? TotalAmount);
