namespace Orders.Application.Orders.Contracts;

public sealed record UpdateOrderRequest(
    string? CustomerName, string? Description, decimal? TotalAmount, string? Status);
