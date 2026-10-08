using Orders.Domain.Common;
using Orders.Domain.Enums;
using Orders.Domain.Rules;

namespace Orders.Domain.Errors;

public static class OrderErrors
{
    private const string ConcurrencyMessage =
        "Este pedido foi alterado por outra pessoa. Recarregue os dados e tente novamente.";

    public static readonly Error NotFound =
        new("order.not_found", "Pedido não encontrado.", ErrorType.NotFound);

    public static readonly Error DeleteNotAllowed =
        new("order.delete_not_allowed", "Pedidos pagos não podem ser excluídos.", ErrorType.Conflict);

    public static readonly Error VersionMismatch =
        new("order.concurrency_conflict", ConcurrencyMessage, ErrorType.PreconditionFailed);

    public static readonly Error ConcurrencyConflict =
        new("order.concurrency_conflict", ConcurrencyMessage, ErrorType.Conflict);

    public static Error ImmutableState(OrderStatus status) => new(
        "order.immutable_state",
        $"Pedidos com status {OrderTransitions.DisplayName(status)} não podem ser alterados.",
        ErrorType.Conflict);
}
