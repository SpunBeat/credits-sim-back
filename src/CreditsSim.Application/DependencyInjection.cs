using CreditsSim.Application.Behaviors;
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

        services.AddScoped<IAmortizationCalculator, FrenchAmortizationCalculator>();
        services.AddScoped<IAmortizationCalculator, GermanAmortizationCalculator>();
        services.AddScoped<IAmortizationCalculatorFactory, AmortizationCalculatorFactory>();

        return services;
