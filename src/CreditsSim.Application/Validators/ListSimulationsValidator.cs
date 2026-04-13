using CreditsSim.Application.Features.Simulations.Queries;
using FluentValidation;

namespace CreditsSim.Application.Validators;

public class ListSimulationsValidator : AbstractValidator<ListSimulationsQuery>
{
    public ListSimulationsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("El numero de pagina debe ser al menos 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("El tamaño de pagina debe estar entre 1 y 100.");

        RuleFor(x => x.SortOrder)
            .Must(s => s is "asc" or "desc")
            .WithMessage("El orden debe ser 'asc' o 'desc'.");

        When(x => x.AmountMin.HasValue && x.AmountMax.HasValue, () =>
        {
            RuleFor(x => x.AmountMin!.Value)
                .LessThanOrEqualTo(x => x.AmountMax!.Value)
                .WithMessage("amountMin no puede ser mayor que amountMax.");
        });

        When(x => x.AnnualRateMin.HasValue && x.AnnualRateMax.HasValue, () =>
        {
            RuleFor(x => x.AnnualRateMin!.Value)
                .LessThanOrEqualTo(x => x.AnnualRateMax!.Value)
                .WithMessage("annualRateMin no puede ser mayor que annualRateMax.");
        });

        When(x => x.TermMonths.HasValue, () =>
        {
            RuleFor(x => x.TermMonths!.Value)
                .InclusiveBetween(1, 360)
                .WithMessage("termMonths debe estar entre 1 y 360.");
        });

        RuleFor(x => x.InstallmentType)
            .Must(t => string.IsNullOrWhiteSpace(t) || t is "FIXED")
            .WithMessage("installmentType no soportado. Use: FIXED.");

        When(x => x.CreatedFrom.HasValue && x.CreatedTo.HasValue, () =>
        {
            RuleFor(x => x.CreatedFrom!.Value)
                .LessThanOrEqualTo(x => x.CreatedTo!.Value)
                .WithMessage("createdFrom debe ser anterior o igual a createdTo.");
        });
    }
}
