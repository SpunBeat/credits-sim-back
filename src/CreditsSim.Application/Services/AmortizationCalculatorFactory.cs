using CreditsSim.Application.DTOs;

namespace CreditsSim.Application.Services;

public interface IAmortizationCalculatorFactory
{
    IAmortizationCalculator GetCalculator(InstallmentType type);
}

public class AmortizationCalculatorFactory : IAmortizationCalculatorFactory
{
    private readonly IEnumerable<IAmortizationCalculator> _calculators;

    public AmortizationCalculatorFactory(IEnumerable<IAmortizationCalculator> calculators)
    {
        _calculators = calculators;
    }

    public IAmortizationCalculator GetCalculator(InstallmentType type)
    {
        var calculator = _calculators.FirstOrDefault(c => c.SupportedType == type);
        if (calculator == null)
            throw new NotSupportedException($"El tipo de cuota '{type}' no está soportado.");

        return calculator;
    }
}
