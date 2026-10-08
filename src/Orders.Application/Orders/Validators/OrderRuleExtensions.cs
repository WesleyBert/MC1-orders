using FluentValidation;
using Orders.Domain.Rules;

namespace Orders.Application.Orders.Validators;

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
