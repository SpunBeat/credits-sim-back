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
            InstallmentType = ParseInstallmentType(entity.InstallmentType),
            Schedule = schedule,
            CreatedAt = entity.CreatedAt
        };
    }

    // La entidad persiste InstallmentType como string por compatibilidad historica.
    // Parseamos al enum en el boundary de application. Si el valor persistido es
    // desconocido, fallback a FIXED (default original del sistema).
    private static InstallmentType ParseInstallmentType(string value) =>
        Enum.TryParse<InstallmentType>(value, ignoreCase: true, out var parsed)
            ? parsed
            : InstallmentType.FIXED;
}
