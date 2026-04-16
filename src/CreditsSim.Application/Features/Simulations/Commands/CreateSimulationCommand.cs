using System.Text.Json;
using CreditsSim.Application.DTOs;
using CreditsSim.Application.Services;
using CreditsSim.Domain.Entities;
using CreditsSim.Domain.Interfaces;
using MediatR;

namespace CreditsSim.Application.Features.Simulations.Commands;

public record CreateSimulationCommand(
    decimal Amount,
    int TermMonths,
    decimal AnnualRate,
    InstallmentType InstallmentType = InstallmentType.FIXED
) : IRequest<SimulationResponse>;

public class CreateSimulationHandler : IRequestHandler<CreateSimulationCommand, SimulationResponse>
{
    private readonly ISimulationRepository _repository;
    private readonly IAmortizationCalculatorFactory _calculatorFactory;

    public CreateSimulationHandler(ISimulationRepository repository, IAmortizationCalculatorFactory calculatorFactory)
    {
        _repository = repository;
        _calculatorFactory = calculatorFactory;
    }

    public async Task<SimulationResponse> Handle(CreateSimulationCommand request, CancellationToken ct)
    {
        var calculator = _calculatorFactory.GetCalculator(request.InstallmentType);
        var schedule = calculator.Calculate(request.Amount, request.TermMonths, request.AnnualRate);

        var entity = new SimulationHistory
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount,
            TermMonths = request.TermMonths,
            AnnualRate = request.AnnualRate,
            InstallmentType = request.InstallmentType.ToString(),
            ScheduleJson = JsonSerializer.Serialize(schedule),
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(entity, ct);

        return new SimulationResponse
        {
            Id = entity.Id,
            Amount = entity.Amount,
            TermMonths = entity.TermMonths,
            AnnualRate = entity.AnnualRate,
            InstallmentType = request.InstallmentType,
            Schedule = schedule,
            CreatedAt = entity.CreatedAt
        };
    }
}
