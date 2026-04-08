namespace CreditsSim.Application.DTOs;

public record SimulationResponse(
    Guid Id,
    decimal Amount,
    int TermMonths,
    decimal AnnualRate,
    string InstallmentType,
    List<ScheduleRow> Schedule,
    DateTime CreatedAt
);

public record ScheduleRow(
    int Month,
    decimal Payment,
    decimal Principal,
    decimal Interest,
    decimal Insurance,
    decimal Balance
);
