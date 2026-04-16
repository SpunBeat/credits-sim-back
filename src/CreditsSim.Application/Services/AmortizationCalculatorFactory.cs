using CreditsSim.Application.Services;

namespace CreditsSim.Application.Services;

public interface IAmortizationCalculatorFactory
{
    IAmortizationCalculator GetCalculator(string type);
}

public class AmortizationCalculatorFactory : IAmortizationCalculatorFactory
{
    private readonly IEnumerable<IAmortizationCalculator> _calculators;

    public AmortizationCalculatorFactory(IEnumerable<IAmortizationCalculator> calculators)
    {
        _calculators = calculators;
    }

    public IAmortizationCalculator GetCalculator(string type)
    {
        var calculator = _calculators.FirstOrDefault(c => string.Equals(c.SupportedType, type, StringComparison.OrdinalIgnoreCase));
        if (calculator == null)
            throw new NotSupportedException($"El tipo de cuota '{type}' no está soportado.");
            
        return calculator;
    }
}
