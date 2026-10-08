using Orders.Domain.Common;

namespace Orders.Domain.Orders;

/// <summary>Catálogo de erros de negócio do pedido.</summary>
public static class OrderErrors
{
    private const string ConcurrencyMessage =
        "Este pedido foi alterado por outra pessoa. Recarregue os dados e tente novamente.";

    public static readonly Error NotFound =
        new("order.not_found", "Pedido não encontrado.", ErrorType.NotFound);

    public static readonly Error DeleteNotAllowed =
        new("order.delete_not_allowed", "Pedidos pagos não podem ser excluídos.", ErrorType.Conflict);

    /// <summary><c>If-Match</c> divergente da versão atual (HTTP 412).</summary>
    public static readonly Error VersionMismatch =
        new("order.concurrency_conflict", ConcurrencyMessage, ErrorType.PreconditionFailed);

    /// <summary>Escrita concorrente persistente mesmo após novas tentativas (HTTP 409).</summary>
    public static readonly Error ConcurrencyConflict =
        new("order.concurrency_conflict", ConcurrencyMessage, ErrorType.Conflict);

    public static Error ImmutableState(OrderStatus status) => new(
        "order.immutable_state",
        $"Pedidos com status {OrderTransitions.DisplayName(status)} não podem ser alterados.",
        ErrorType.Conflict);
}
