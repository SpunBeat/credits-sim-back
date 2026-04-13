using CreditsSim.Domain.Entities;
using CreditsSim.Domain.Query;
using CreditsSim.Domain.Specifications;

namespace CreditsSim.Infrastructure.Persistence.Specifications;

internal static class SimulationQueryableExtensions
{
    /// <summary>
    /// Aplica filtros de forma incremental (AND), sin modificar el <see cref="IQueryable"/> base cuando no hay criterios.
    /// </summary>
    public static IQueryable<SimulationHistory> ApplySimulationFilters(
        this IQueryable<SimulationHistory> query,
        SimulationListFilter? filter)
    {
        if (filter is null)
            return query;

        return query
            .Apply(new AmountMinSpecification(filter.AmountMin))
            .Apply(new AmountMaxSpecification(filter.AmountMax))
            .Apply(new TermMonthsSpecification(filter.TermMonths))
            .Apply(new AnnualRateMinSpecification(filter.AnnualRateMin))
            .Apply(new AnnualRateMaxSpecification(filter.AnnualRateMax))
            .Apply(new InstallmentTypeSpecification(filter.InstallmentType))
            .Apply(new CreatedFromSpecification(filter.CreatedFrom))
            .Apply(new CreatedToSpecification(filter.CreatedTo));
    }

    private static IQueryable<SimulationHistory> Apply(
        this IQueryable<SimulationHistory> query,
        ISpecification<SimulationHistory> specification) =>
        specification.Apply(query);
}
