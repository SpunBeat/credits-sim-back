using CreditsSim.Application.Behaviors;
using CreditsSim.Application.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CreditsSim.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // IAmortizationCalculator se registra multiples veces intencionalmente.
        // AmortizationCalculatorFactory recibe IEnumerable<IAmortizationCalculator>
        // y selecciona por SupportedType. NO inyectar IAmortizationCalculator
        // directo en constructores — siempre usar el factory.
        // TODO: Migrar a keyed services (.NET 8) — ver ticket de follow-up.
        services.AddScoped<IAmortizationCalculator, FrenchAmortizationCalculator>();
        services.AddScoped<IAmortizationCalculator, GermanAmortizationCalculator>();
        services.AddScoped<IAmortizationCalculatorFactory, AmortizationCalculatorFactory>();

        return services;
    }
}
