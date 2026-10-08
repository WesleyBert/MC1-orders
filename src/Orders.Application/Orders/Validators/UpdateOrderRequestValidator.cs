using FluentValidation;
using Orders.Application.Orders.Contracts;

namespace Orders.Application.Orders.Validators;

public sealed class UpdateOrderRequestValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        // Valida o texto já aparado: "   " conta como vazio, igual ao que o domínio grava.
        RuleFor(x => OrderRuleExtensions.TrimOrNull(x.CustomerName)).CustomerNameRules()
            .OverridePropertyName(nameof(UpdateOrderRequest.CustomerName));
        RuleFor(x => OrderRuleExtensions.TrimOrNull(x.Description)).DescriptionRules()
            .OverridePropertyName(nameof(UpdateOrderRequest.Description));
        RuleFor(x => x.TotalAmount).TotalAmountRules();

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("O status é obrigatório.")
            .Must(s => OrderParsing.ParseStatus(s) is not null)
            .WithMessage("Status inválido. Valores aceitos: Open, Paid, Cancelled.");
    }
}
