using CreditsSim.Domain.Entities;
using CreditsSim.Domain.Query;

namespace CreditsSim.Domain.Interfaces;

public interface ISimulationRepository
{
    Task<SimulationHistory> AddAsync(SimulationHistory simulation, CancellationToken ct = default);
    Task<SimulationHistory?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Cursor-based pagination. Fetches pageSize+1 rows to detect hasNextPage without COUNT.
    /// </summary>
    Task<(List<SimulationHistory> Items, bool HasNextPage)> GetCursorPagedAsync(
        int pageSize,
        bool ascending = false,
        DateTime? cursorCreatedAt = null,
        Guid? cursorId = null,
        SimulationListFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Optional total count for UI (can be expensive on large tables).
    /// </summary>
    Task<int> CountAsync(SimulationListFilter? filter = null, CancellationToken ct = default);
}
