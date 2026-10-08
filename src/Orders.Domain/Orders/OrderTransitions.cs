namespace Orders.Domain.Orders;

/// <summary>
/// Máquina de estados do pedido: Open → Paid | Cancelled. Paid e Cancelled são finais.
/// Única fonte da verdade para transições, edição e exclusão.
/// </summary>
public static class OrderTransitions
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [OrderStatus.Open] = [OrderStatus.Open, OrderStatus.Paid, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [],
        [OrderStatus.Cancelled] = [],
    };

    public static bool IsFinal(OrderStatus status) => Allowed[status].Length == 0;

    public static bool CanTransition(OrderStatus from, OrderStatus to) => Allowed[from].Contains(to);

    public static IReadOnlyList<OrderStatus> AllowedTargets(OrderStatus from) => Allowed[from];

    /// <summary>Pedido pago é registro financeiro e não pode ser excluído.</summary>
    public static bool CanDelete(OrderStatus status) => status != OrderStatus.Paid;

    /// <summary>Rótulo em pt-BR usado nas mensagens de negócio.</summary>
    public static string DisplayName(OrderStatus status) => status switch
    {
        OrderStatus.Open => "Aberto",
        OrderStatus.Paid => "Pago",
        OrderStatus.Cancelled => "Cancelado",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
