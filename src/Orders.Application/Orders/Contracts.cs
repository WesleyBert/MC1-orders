using Orders.Domain.Orders;

namespace Orders.Application.Orders;

// Campos anuláveis: distinguem "não enviado" (obrigatório) de "enviado inválido".
// Status como string: aceita só nomes (case-insensitive) e devolve mensagem própria em pt-BR.

/// <summary>Corpo de <c>POST /api/v1/orders</c>. Status inicial é sempre Open.</summary>
public sealed record CreateOrderRequest(string? CustomerName, string? Description, decimal? TotalAmount);

/// <summary>Corpo de <c>PUT /api/v1/orders/{id}</c>.</summary>
public sealed record UpdateOrderRequest(
    string? CustomerName, string? Description, decimal? TotalAmount, string? Status);

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

public sealed record OrderResponse(
    Guid Id,
    long Number,
    string CustomerName,
    string Description,
    decimal TotalAmount,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long Version)
{
    public static OrderResponse From(Order order) => new(
        order.Id,
        order.Number,
        order.CustomerName,
        order.Description,
        order.TotalAmount,
        order.Status,
        order.CreatedAt,
        order.UpdatedAt,
        order.Version);
}

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems)
{
    public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
