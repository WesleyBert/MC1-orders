using FluentValidation;
using Orders.Application.Orders.Contracts;

namespace Orders.Application.Orders.Validators;

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
