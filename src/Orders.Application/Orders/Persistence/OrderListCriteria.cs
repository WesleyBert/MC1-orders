using Orders.Domain.Orders;

namespace Orders.Application.Orders.Persistence;

/// <summary>Critérios de listagem já validados e convertidos para tipos do domínio.</summary>
public sealed record OrderListCriteria(
    string? Search,
    OrderStatus? Status,
    int Page,
    int PageSize,
    OrderSortField SortBy,
    bool Descending);
