using CreditsSim.Application.DTOs;

namespace CreditsSim.Application.Services;

/// <summary>
/// Clase base para calculadoras de amortizacion. Centraliza la tasa de seguro
/// mensual para evitar duplicacion entre sistemas (frances, aleman, futuros).
/// </summary>
public abstract class AmortizationCalculatorBase : IAmortizationCalculator
{
    // TODO: Mover a IOptions<AmortizationSettings> — ver ticket de follow-up.
    protected const decimal InsuranceRateMonthly = 0.00065m; // 0.065% mensual sobre saldo

    public abstract InstallmentType SupportedType { get; }

    public abstract List<ScheduleRow> Calculate(decimal amount, int termMonths, decimal annualRate);
}
