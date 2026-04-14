using CreditsSim.Domain.Entities;
using CreditsSim.Domain.Query;

namespace CreditsSim.Domain.Interfaces;

public interface ISimulationRepository
{
    Task<SimulationHistory> AddAsync(SimulationHistory simulation, CancellationToken ct = default);
    Task<SimulationHistory?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<(List<SimulationHistory> Items, bool HasNextPage)> GetCursorPagedAsync(
        int pageSize,
        bool ascending = false,
        string sortBy = "createdAt",
        DateTime? cursorCreatedAt = null,
        Guid? cursorId = null,
        SimulationListFilter? filter = null,
        CancellationToken ct = default);

    Task<int> CountAsync(SimulationListFilter? filter = null, CancellationToken ct = default);
}
