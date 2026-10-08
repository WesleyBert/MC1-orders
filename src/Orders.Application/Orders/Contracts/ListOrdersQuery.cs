namespace Orders.Application.Orders.Contracts;

/// <summary>Parâmetros de <c>GET /api/v1/orders</c>, como chegam da query string.</summary>
public sealed record ListOrdersQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int SearchMaxLength = 100;

    public string? Search { get; init; }

    public string? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;

    public string? SortBy { get; init; }

    public string? SortDir { get; init; }
}
