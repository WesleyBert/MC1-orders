namespace Orders.Application.Common;

/// <summary>Itens de uma página e o total de itens que atendem ao filtro.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalItems);
