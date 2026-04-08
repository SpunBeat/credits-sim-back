namespace CreditsSim.Application.DTOs;

public record SimulationRequest(
    decimal Amount,
    int TermMonths,
    decimal AnnualRate,
    string InstallmentType = "FIXED"
);
