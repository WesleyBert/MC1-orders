using Orders.Application.Orders.Persistence;
using Orders.Domain.Orders;

namespace Orders.Application.Orders;

/// <summary>Converte textos da API em tipos do domínio, aceitando apenas nomes (nunca números).</summary>
public static class OrderParsing
{
    public static OrderStatus? ParseStatus(string? value) => ParseName<OrderStatus>(value);

    public static OrderSortField? ParseSortField(string? value) => ParseName<OrderSortField>(value);

    /// <returns><c>true</c> para desc, <c>false</c> para asc, <c>null</c> se inválido.</returns>
    public static bool? ParseSortDescending(string? value) => value?.ToUpperInvariant() switch
    {
        "DESC" => true,
        "ASC" => false,
        _ => null,
    };

    // Enum.TryParse aceitaria "2" ou "1,2"; exigir só letras evita acoplar clientes à ordem do enum.
    private static TEnum? ParseName<TEnum>(string? value)
        where TEnum : struct, Enum =>
        !string.IsNullOrEmpty(value)
        && value.All(char.IsLetter)
        && Enum.TryParse<TEnum>(value, ignoreCase: true, out var result)
        && Enum.IsDefined(result)
            ? result
            : null;
}
