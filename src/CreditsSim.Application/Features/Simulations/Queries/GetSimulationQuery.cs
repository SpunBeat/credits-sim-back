using System.Text.Json;
using CreditsSim.Application.DTOs;
using CreditsSim.Domain.Interfaces;
using MediatR;

namespace CreditsSim.Application.Features.Simulations.Queries;

public record GetSimulationQuery(Guid Id) : IRequest<SimulationResponse?>;

public class GetSimulationHandler : IRequestHandler<GetSimulationQuery, SimulationResponse?>
{
    private readonly ISimulationRepository _repository;

    public GetSimulationHandler(ISimulationRepository repository)
    {
        _repository = repository;
    }

    public async Task<SimulationResponse?> Handle(GetSimulationQuery request, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(request.Id, ct);
        if (entity is null) return null;

        var schedule = JsonSerializer.Deserialize<List<ScheduleRow>>(entity.ScheduleJson) ?? [];

        return new SimulationResponse
        {
            Id = entity.Id,
            Amount = entity.Amount,
            TermMonths = entity.TermMonths,
            AnnualRate = entity.AnnualRate,
            InstallmentType = entity.InstallmentType,
            Schedule = schedule,
            CreatedAt = entity.CreatedAt
        };
    }
}
