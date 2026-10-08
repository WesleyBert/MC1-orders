using Orders.Domain.Enums;

namespace Orders.Application.Orders.Persistence;

public sealed record OrderListCriteria(
    string? Search,
    OrderStatus? Status,
    int Page,
    int PageSize,
    OrderSortField SortBy,
    bool Descending);
