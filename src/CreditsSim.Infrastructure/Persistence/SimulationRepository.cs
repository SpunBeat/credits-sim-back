using System.Linq.Expressions;
using CreditsSim.Domain.Entities;
using CreditsSim.Domain.Interfaces;
using CreditsSim.Domain.Query;
using CreditsSim.Infrastructure.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace CreditsSim.Infrastructure.Persistence;

public class SimulationRepository : ISimulationRepository
{
    private readonly AppDbContext _context;

    private static readonly Dictionary<string, Expression<Func<SimulationHistory, object>>> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["createdAt"] = s => s.CreatedAt,
        ["amount"] = s => s.Amount,
        ["termMonths"] = s => s.TermMonths,
        ["annualRate"] = s => s.AnnualRate,
        ["installmentType"] = s => s.InstallmentType,
    };

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

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var rows = await _context.SimulationHistories
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync(ct);

        return rows > 0;
    }

    public async Task<(List<SimulationHistory> Items, bool HasNextPage)> GetCursorPagedAsync(
        int pageSize,
        bool ascending = false,
        string sortBy = "createdAt",
        DateTime? cursorCreatedAt = null,
        Guid? cursorId = null,
        SimulationListFilter? filter = null,
        CancellationToken ct = default)
    {
        var query = _context.SimulationHistories
            .AsNoTracking()
            .ApplySimulationFilters(filter);

        // Cursor condition (always on CreatedAt+Id regardless of sortBy)
        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            if (ascending)
            {
                query = query.Where(s =>
                    s.CreatedAt > cursorCreatedAt.Value ||
                    (s.CreatedAt == cursorCreatedAt.Value && s.Id > cursorId.Value));
            }
            else
            {
                query = query.Where(s =>
                    s.CreatedAt < cursorCreatedAt.Value ||
                    (s.CreatedAt == cursorCreatedAt.Value && s.Id < cursorId.Value));
            }
        }

        // Dynamic sort by requested column, then tiebreaker on (CreatedAt, Id)
        var sortExpr = SortColumns.GetValueOrDefault(sortBy)
                       ?? SortColumns["createdAt"];

        query = ascending
            ? query.OrderBy(sortExpr).ThenBy(s => s.CreatedAt).ThenBy(s => s.Id)
            : query.OrderByDescending(sortExpr).ThenByDescending(s => s.CreatedAt).ThenByDescending(s => s.Id);

        var rows = await query
            .Take(pageSize + 1)
            .ToListAsync(ct);

        var hasNextPage = rows.Count > pageSize;
        if (hasNextPage)
        {
            rows.RemoveAt(rows.Count - 1);
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
