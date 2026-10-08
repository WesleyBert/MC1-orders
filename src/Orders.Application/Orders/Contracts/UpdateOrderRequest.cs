namespace Orders.Application.Orders.Contracts;

/// <summary>
/// Corpo de <c>PUT /api/v1/orders/{id}</c>. Status como string: aceita só nomes
/// (case-insensitive) e devolve mensagem própria em pt-BR.
/// </summary>
public sealed record UpdateOrderRequest(
    string? CustomerName, string? Description, decimal? TotalAmount, string? Status);
