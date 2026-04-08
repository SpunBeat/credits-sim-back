using CreditsSim.Domain.Entities;

namespace CreditsSim.Domain.Interfaces;

public interface ISimulationRepository
{
    Task<SimulationHistory> AddAsync(SimulationHistory simulation, CancellationToken ct = default);
    Task<SimulationHistory?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
