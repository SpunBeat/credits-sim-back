using CreditsSim.Application.DTOs;

namespace CreditsSim.Application.Services;

public static class AmortizationService
{
    private const decimal InsuranceRateMonthly = 0.00065m; // 0.065% mensual sobre saldo

    /// <summary>
    /// Calcula el cronograma de pagos usando el Sistema Francés (cuotas constantes).
    /// Fórmula: C = P * [r(1+r)^n] / [(1+r)^n - 1]
    /// </summary>
    public static List<ScheduleRow> CalculateSchedule(decimal amount, int termMonths, decimal annualRate)
    {
        var monthlyRate = annualRate / 12m / 100m;
        var schedule = new List<ScheduleRow>(termMonths);

        decimal fixedPayment;
        if (monthlyRate == 0)
        {
            fixedPayment = amount / termMonths;
        }
        else
        {
            var factor = (double)monthlyRate * Math.Pow(1 + (double)monthlyRate, termMonths)
                         / (Math.Pow(1 + (double)monthlyRate, termMonths) - 1);
            fixedPayment = amount * (decimal)factor;
        }

        fixedPayment = Math.Round(fixedPayment, 2);
        var balance = amount;

        for (var month = 1; month <= termMonths; month++)
        {
            var interest = Math.Round(balance * monthlyRate, 2);
            var insurance = Math.Round(balance * InsuranceRateMonthly, 2);
            var principal = fixedPayment - interest;

            if (month == termMonths)
            {
                principal = balance;
                fixedPayment = principal + interest;
            }

            balance -= principal;
            if (balance < 0) balance = 0;

            schedule.Add(new ScheduleRow
            {
                Month = month,
                Payment = fixedPayment + insurance,
                Principal = principal,
                Interest = interest,
                Insurance = insurance,
                Balance = Math.Round(balance, 2)
            });
        }

        return schedule;
    }
}
