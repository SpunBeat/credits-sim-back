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
    string InstallmentType = "FIXED"
) : IRequest<SimulationResponse>;

public class CreateSimulationHandler : IRequestHandler<CreateSimulationCommand, SimulationResponse>
{
    private readonly ISimulationRepository _repository;

    public CreateSimulationHandler(ISimulationRepository repository)
    {
        _repository = repository;
    }

    public async Task<SimulationResponse> Handle(CreateSimulationCommand request, CancellationToken ct)
    {
        var schedule = AmortizationService.CalculateSchedule(
            request.Amount, request.TermMonths, request.AnnualRate);

        var entity = new SimulationHistory
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount,
            TermMonths = request.TermMonths,
            AnnualRate = request.AnnualRate,
            InstallmentType = request.InstallmentType,
            ScheduleJson = JsonSerializer.Serialize(schedule),
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(entity, ct);

        return new SimulationResponse(
            entity.Id,
            entity.Amount,
            entity.TermMonths,
            entity.AnnualRate,
            entity.InstallmentType,
            schedule,
            entity.CreatedAt
        );
    }
}
