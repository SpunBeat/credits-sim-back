using CreditsSim.Domain.Interfaces;
using MediatR;

namespace CreditsSim.Application.Features.Simulations.Commands;

/// <summary>
/// Elimina una simulación por su ID. Retorna true si se eliminó, false si no existía.
/// </summary>
public record DeleteSimulationCommand(Guid Id) : IRequest<bool>;

public class DeleteSimulationHandler : IRequestHandler<DeleteSimulationCommand, bool>
{
    private readonly ISimulationRepository _repository;

    public DeleteSimulationHandler(ISimulationRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteSimulationCommand request, CancellationToken ct)
    {
        return await _repository.DeleteAsync(request.Id, ct);
    }
}
