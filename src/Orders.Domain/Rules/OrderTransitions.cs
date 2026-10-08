using Orders.Domain.Enums;

namespace Orders.Domain.Rules;

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

    public static bool CanDelete(OrderStatus status) => status != OrderStatus.Paid;

    public static string DisplayName(OrderStatus status) => status switch
    {
        OrderStatus.Open => "Aberto",
        OrderStatus.Paid => "Pago",
        OrderStatus.Cancelled => "Cancelado",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
