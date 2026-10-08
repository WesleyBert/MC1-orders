using FluentValidation;
using Orders.Application.Orders.Contracts;

namespace Orders.Application.Orders.Validators;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => OrderRuleExtensions.TrimOrNull(x.CustomerName)).CustomerNameRules()
            .OverridePropertyName(nameof(CreateOrderRequest.CustomerName));
        RuleFor(x => OrderRuleExtensions.TrimOrNull(x.Description)).DescriptionRules()
            .OverridePropertyName(nameof(CreateOrderRequest.Description));
        RuleFor(x => x.TotalAmount).TotalAmountRules();
    }
}
