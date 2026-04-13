namespace CreditsSim.Domain.Query;

/// <summary>
/// Criterios opcionales de filtrado para el listado de simulaciones (todos AND).
/// </summary>
public record SimulationListFilter(
    decimal? AmountMin = null,
    decimal? AmountMax = null,
    int? TermMonths = null,
    decimal? AnnualRateMin = null,
    decimal? AnnualRateMax = null,
    string? InstallmentType = null,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null);
