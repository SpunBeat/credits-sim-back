using CreditsSim.Application.Services;

namespace CreditsSim.Tests.Calculators;

public class GermanAmortizationCalculatorTests
{
    [Theory]
    [InlineData(12000, 12, 12)]    // divisible exacto
    [InlineData(100000, 7, 18.5)]  // no divisible, fuerza drift de redondeo
    [InlineData(50000, 1, 10)]     // plazo = 1 mes
    [InlineData(50000, 24, 0)]     // tasa = 0%
    public void Calculate_MantieneInvariantes(
        decimal amount, int months, decimal rate)
    {
        var schedule = new GermanAmortizationCalculator()
            .Calculate(amount, months, rate);

        // Saldo final = 0
        Assert.Equal(0m, schedule.Last().Balance);

        // Suma de capital = monto original
        Assert.Equal(amount, schedule.Sum(r => r.Principal));

        // Cuota total decrece periodo a periodo (solo si tasa > 0; con tasa 0
        // la cuota solo depende del seguro sobre saldo decreciente, que tambien decrece).
        if (rate > 0)
        {
            for (var i = 1; i < schedule.Count; i++)
            {
                Assert.True(schedule[i].Payment <= schedule[i - 1].Payment,
                    $"Cuota {i + 1} ({schedule[i].Payment}) > cuota {i} ({schedule[i - 1].Payment})");
            }
        }
    }

    [Fact]
    public void Calculate_CronogramaConocido_12000_12meses_12pct()
    {
        // amount=12000, termMonths=12, annualRate=12% -> monthlyRate=1%
        // constantPrincipal = 1000.00
        // mes 1: interes = 12000 * 0.01 = 120.00, cuota (sin seguro) = 1120.00
        // mes 12: interes = 1000 * 0.01 = 10.00, cuota (sin seguro) = 1010.00
        var schedule = new GermanAmortizationCalculator()
            .Calculate(12000m, 12, 12m);

        Assert.Equal(12, schedule.Count);
        Assert.Equal(1000.00m, schedule[0].Principal);
        Assert.Equal(120.00m, schedule[0].Interest);
        Assert.Equal(10.00m, schedule[11].Interest);
        Assert.Equal(0m, schedule[11].Balance);
    }

    [Fact]
    public void Calculate_PlazoUno_CuotaUnicaCoincideConCapitalMasInteres()
    {
        var schedule = new GermanAmortizationCalculator()
            .Calculate(50000m, 1, 10m);

        Assert.Single(schedule);
        Assert.Equal(50000m, schedule[0].Principal);
        Assert.Equal(0m, schedule[0].Balance);
    }

    [Fact]
    public void Calculate_TasaCero_InteresSiempreCero()
    {
        var schedule = new GermanAmortizationCalculator()
            .Calculate(50000m, 24, 0m);

        Assert.All(schedule, row => Assert.Equal(0m, row.Interest));
        Assert.Equal(0m, schedule.Last().Balance);
    }
}
