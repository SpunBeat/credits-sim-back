using CreditsSim.Application.Features.Simulations.Commands;
using FluentValidation;

namespace CreditsSim.Application.Validators;

public class CreateSimulationValidator : AbstractValidator<CreateSimulationCommand>
{
    public CreateSimulationValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0.")
            .LessThanOrEqualTo(10_000_000).WithMessage("El monto no puede exceder 10,000,000.");

        RuleFor(x => x.TermMonths)
            .InclusiveBetween(1, 360).WithMessage("El plazo debe estar entre 1 y 360 meses.");

        RuleFor(x => x.AnnualRate)
            .GreaterThanOrEqualTo(0).WithMessage("La tasa anual no puede ser negativa.")
            .LessThanOrEqualTo(100).WithMessage("La tasa anual no puede exceder 100%.");

        // El tipo es un enum C#: JsonStringEnumConverter rechaza strings invalidos ("german",
        // null, desconocidos) antes de llegar al validator con JsonException -> 400.
        // IsInEnum() es defensivo contra valores numericos fuera de rango en JSON (ej: 99).
        RuleFor(x => x.InstallmentType)
            .IsInEnum().WithMessage("Tipo de cuota no soportado. Use: FIXED, GERMAN.");
    }
}
