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
    }
}
