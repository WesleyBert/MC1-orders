using FluentValidation;
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

/// <summary>Regras compartilhadas entre criação e atualização (limites em <see cref="OrderRules"/>).</summary>
internal static class OrderRuleExtensions
{
    public static IRuleBuilderOptions<T, string?> CustomerNameRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .NotEmpty().WithMessage("O nome do cliente é obrigatório.")
            .Length(OrderRules.CustomerNameMinLength, OrderRules.CustomerNameMaxLength)
            .WithMessage($"O nome do cliente deve ter entre {OrderRules.CustomerNameMinLength} e {OrderRules.CustomerNameMaxLength} caracteres.");

    public static IRuleBuilderOptions<T, string?> DescriptionRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .Length(OrderRules.DescriptionMinLength, OrderRules.DescriptionMaxLength)
            .WithMessage($"A descrição deve ter entre {OrderRules.DescriptionMinLength} e {OrderRules.DescriptionMaxLength} caracteres.");

    public static IRuleBuilderOptions<T, decimal?> TotalAmountRules<T>(this IRuleBuilder<T, decimal?> rule) =>
        rule
            .NotNull().WithMessage("O valor total é obrigatório.")
            .GreaterThan(0).WithMessage("O valor total deve ser maior que zero.")
            .LessThanOrEqualTo(OrderRules.TotalAmountMax)
            .WithMessage("O valor total deve ser no máximo R$ 999.999.999,99.")
            .PrecisionScale(11, OrderRules.TotalAmountMaxDecimals, ignoreTrailingZeros: true)
            .WithMessage("O valor total deve ter no máximo 2 casas decimais.");

    public static string? TrimOrNull(string? value) => value?.Trim();
}

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        // Valida o texto já aparado: "   " conta como vazio, igual ao que o domínio grava.
        RuleFor(x => OrderRuleExtensions.TrimOrNull(x.CustomerName)).CustomerNameRules()
            .OverridePropertyName("CustomerName");
        RuleFor(x => OrderRuleExtensions.TrimOrNull(x.Description)).DescriptionRules()
            .OverridePropertyName("Description");
        RuleFor(x => x.TotalAmount).TotalAmountRules();
    }
}

public sealed class UpdateOrderRequestValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        // Valida o texto já aparado: "   " conta como vazio, igual ao que o domínio grava.
        RuleFor(x => OrderRuleExtensions.TrimOrNull(x.CustomerName)).CustomerNameRules()
            .OverridePropertyName("CustomerName");
        RuleFor(x => OrderRuleExtensions.TrimOrNull(x.Description)).DescriptionRules()
            .OverridePropertyName("Description");
        RuleFor(x => x.TotalAmount).TotalAmountRules();

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("O status é obrigatório.")
            .Must(s => OrderParsing.ParseStatus(s) is not null)
            .WithMessage("Status inválido. Valores aceitos: Open, Paid, Cancelled.");
    }
}

public sealed class ListOrdersQueryValidator : AbstractValidator<ListOrdersQuery>
{
    public ListOrdersQueryValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(ListOrdersQuery.SearchMaxLength)
            .WithMessage($"O termo de busca deve ter no máximo {ListOrdersQuery.SearchMaxLength} caracteres.");

        RuleFor(x => x.Status)
            .Must(s => OrderParsing.ParseStatus(s) is not null)
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("Status inválido. Valores aceitos: Open, Paid, Cancelled.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("A página deve ser maior ou igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, ListOrdersQuery.MaxPageSize)
            .WithMessage($"O tamanho da página deve estar entre 1 e {ListOrdersQuery.MaxPageSize}.");

        RuleFor(x => x.SortBy)
            .Must(s => OrderParsing.ParseSortField(s) is not null)
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage("Campo de ordenação inválido. Valores aceitos: createdAt, number, customerName, totalAmount, status.");

        RuleFor(x => x.SortDir)
            .Must(s => OrderParsing.ParseSortDescending(s) is not null)
            .When(x => !string.IsNullOrWhiteSpace(x.SortDir))
            .WithMessage("Direção de ordenação inválida. Use asc ou desc.");
    }
}
