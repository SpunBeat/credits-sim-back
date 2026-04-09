using CreditsSim.Domain.Entities;
using CreditsSim.Domain.Interfaces;
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

    public async Task<(List<SimulationHistory> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        bool ascending = false,
        CancellationToken ct = default)
    {
        var query = _context.SimulationHistories.AsNoTracking();

        var totalCount = await query.CountAsync(ct);

        query = ascending
            ? query.OrderBy(s => s.CreatedAt)
            : query.OrderByDescending(s => s.CreatedAt);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
