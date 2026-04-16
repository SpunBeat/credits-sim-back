using CreditsSim.Application.DTOs;
using CreditsSim.Application.Services;

namespace CreditsSim.Tests.Calculators;

public class AmortizationCalculatorFactoryTests
{
    private static AmortizationCalculatorFactory BuildFactory() =>
        new(new IAmortizationCalculator[]
        {
            new FrenchAmortizationCalculator(),
            new GermanAmortizationCalculator()
        });

    [Fact]
    public void GetCalculator_FIXED_ReturnsFrench()
    {
        var factory = BuildFactory();
        var calc = factory.GetCalculator(InstallmentType.FIXED);
        Assert.IsType<FrenchAmortizationCalculator>(calc);
    }

    [Fact]
    public void GetCalculator_GERMAN_ReturnsGerman()
    {
        var factory = BuildFactory();
        var calc = factory.GetCalculator(InstallmentType.GERMAN);
        Assert.IsType<GermanAmortizationCalculator>(calc);
    }

    [Fact]
    public void GetCalculator_TipoInvalido_Throws()
    {
        var factory = BuildFactory();
        Assert.Throws<NotSupportedException>(
            () => factory.GetCalculator((InstallmentType)999));
    }
}
