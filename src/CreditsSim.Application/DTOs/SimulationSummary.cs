namespace CreditsSim.Application.DTOs;

public record SimulationSummary(
    Guid Id,
    decimal Amount,
    int TermMonths,
    decimal AnnualRate,
    string InstallmentType,
    DateTime CreatedAt
);
