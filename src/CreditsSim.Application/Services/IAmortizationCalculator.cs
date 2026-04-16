using CreditsSim.Application.DTOs;

namespace CreditsSim.Application.Services;

public interface IAmortizationCalculator
{
    InstallmentType SupportedType { get; }
    List<ScheduleRow> Calculate(decimal amount, int termMonths, decimal annualRate);
}
