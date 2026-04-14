using CreditsSim.Domain.Entities;
using CreditsSim.Domain.Interfaces;
using CreditsSim.Domain.Query;
using CreditsSim.Infrastructure.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace CreditsSim.Infrastructure.Persistence;

public class SimulationRepository : ISimulationRepository
{
    private readonly AppDbContext _context;

    public SimulationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SimulationHistory> AddAsync(SimulationHistory simulation, CancellationToken ct = default)
    {
        _context.SimulationHistories.Add(simulation);
        await _context.SaveChangesAsync(ct);
        return simulation;
    }

    public async Task<SimulationHistory?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.SimulationHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<(List<SimulationHistory> Items, bool HasNextPage)> GetCursorPagedAsync(
        int pageSize,
        bool ascending = false,
        DateTime? cursorCreatedAt = null,
        Guid? cursorId = null,
        SimulationListFilter? filter = null,
        CancellationToken ct = default)
    {
        var query = _context.SimulationHistories
            .AsNoTracking()
            .ApplySimulationFilters(filter);

        // Apply cursor condition (keyset pagination)
        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            if (ascending)
            {
                // Next page ascending: createdAt > cursor OR (createdAt == cursor AND id > cursorId)
                query = query.Where(s =>
                    s.CreatedAt > cursorCreatedAt.Value ||
                    (s.CreatedAt == cursorCreatedAt.Value && s.Id > cursorId.Value));
            }
            else
            {
                // Next page descending: createdAt < cursor OR (createdAt == cursor AND id < cursorId)
                query = query.Where(s =>
                    s.CreatedAt < cursorCreatedAt.Value ||
                    (s.CreatedAt == cursorCreatedAt.Value && s.Id < cursorId.Value));
            }
        }

        // Order by (CreatedAt, Id) — consistent with the composite index
        query = ascending
            ? query.OrderBy(s => s.CreatedAt).ThenBy(s => s.Id)
            : query.OrderByDescending(s => s.CreatedAt).ThenByDescending(s => s.Id);

        // Fetch one extra row to detect if there's a next page
        var rows = await query
            .Take(pageSize + 1)
            .ToListAsync(ct);

        var hasNextPage = rows.Count > pageSize;
        if (hasNextPage)
        {
            rows.RemoveAt(rows.Count - 1); // Remove the extra sentinel row
        }

        return (rows, hasNextPage);
    }

    public async Task<int> CountAsync(SimulationListFilter? filter = null, CancellationToken ct = default)
    {
        return await _context.SimulationHistories
            .AsNoTracking()
            .ApplySimulationFilters(filter)
            .CountAsync(ct);
    }
}
