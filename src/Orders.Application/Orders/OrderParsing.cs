using Orders.Application.Orders.Persistence;
using Orders.Domain.Enums;

namespace Orders.Application.Orders;

public static class OrderParsing
{
    public static OrderStatus? ParseStatus(string? value) => ParseName<OrderStatus>(value);

    public static OrderSortField? ParseSortField(string? value) => ParseName<OrderSortField>(value);

    public static bool? ParseSortDescending(string? value) => value?.ToUpperInvariant() switch
    {
        "DESC" => true,
        "ASC" => false,
        _ => null,
    };

    private static TEnum? ParseName<TEnum>(string? value)
        where TEnum : struct, Enum =>
        !string.IsNullOrEmpty(value)
        && value.All(char.IsLetter)
        && Enum.TryParse<TEnum>(value, ignoreCase: true, out var result)
        && Enum.IsDefined(result)
            ? result
            : null;
}
