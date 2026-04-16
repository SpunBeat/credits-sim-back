using CreditsSim.Application.DTOs;

namespace CreditsSim.Application.Services;

public class GermanAmortizationCalculator : AmortizationCalculatorBase
{
    public override InstallmentType SupportedType => InstallmentType.GERMAN;

    /// <summary>
    /// Calcula el cronograma de pagos usando el Sistema Alemán (cuota de capital constante).
    /// Capital = P / n, Interés = Saldo * r, Cuota total = Capital + Interés + Seguro.
    /// La cuota total decrece período a período porque el interés se aplica sobre saldo decreciente.
    /// </summary>
    /// <remarks>
    /// El capital por período = monto / plazo, redondeado a 2 decimales.
    /// El último período absorbe el drift de redondeo: principal = saldo pendiente.
    /// El saldo siempre cierra en 0.
    /// </remarks>
    public override List<ScheduleRow> Calculate(decimal amount, int termMonths, decimal annualRate)
    {
        var monthlyRate = annualRate / 12m / 100m;
        var schedule = new List<ScheduleRow>(termMonths);

        var constantPrincipal = Math.Round(amount / termMonths, 2);
        var balance = amount;

        for (var month = 1; month <= termMonths; month++)
        {
            var interest = Math.Round(balance * monthlyRate, 2);
            var insurance = Math.Round(balance * InsuranceRateMonthly, 2);
            var principal = constantPrincipal;

            if (month == termMonths)
            {
                principal = balance;
            }

            var payment = principal + interest + insurance;

            balance -= principal;
            if (balance < 0) balance = 0;

            schedule.Add(new ScheduleRow
            {
                Month = month,
                Payment = payment,
                Principal = principal,
                Interest = interest,
                Insurance = insurance,
                Balance = Math.Round(balance, 2)
            });
        }

        return schedule;
    }
}
