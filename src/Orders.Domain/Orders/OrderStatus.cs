namespace Orders.Domain.Orders;

/// <summary>
/// Status do pedido. Começa em 1 para que <c>default(OrderStatus)</c> seja inválido e detectável.
/// </summary>
public enum OrderStatus
{
    Open = 1,
    Paid = 2,
    Cancelled = 3,
}
