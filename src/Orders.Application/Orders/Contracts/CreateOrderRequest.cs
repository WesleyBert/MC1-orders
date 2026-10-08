namespace Orders.Application.Orders.Contracts;

/// <summary>
/// Corpo de <c>POST /api/v1/orders</c>. Status inicial é sempre Open.
/// Campos anuláveis distinguem "não enviado" (obrigatório) de "enviado inválido".
/// </summary>
public sealed record CreateOrderRequest(string? CustomerName, string? Description, decimal? TotalAmount);
