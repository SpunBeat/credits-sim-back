using CreditsSim.Domain.Entities;
using CreditsSim.Domain.Specifications;
using Microsoft.EntityFrameworkCore;

namespace CreditsSim.Infrastructure.Persistence.Specifications;

internal sealed class AmountMinSpecification : ISpecification<SimulationHistory>
{
    private readonly decimal? _min;

    public AmountMinSpecification(decimal? min) => _min = min;

    public IQueryable<SimulationHistory> Apply(IQueryable<SimulationHistory> query) =>
        _min is null ? query : query.Where(s => s.Amount >= _min.Value);
}

internal sealed class AmountMaxSpecification : ISpecification<SimulationHistory>
{
    private readonly decimal? _max;

    public AmountMaxSpecification(decimal? max) => _max = max;

    public IQueryable<SimulationHistory> Apply(IQueryable<SimulationHistory> query) =>
        _max is null ? query : query.Where(s => s.Amount <= _max.Value);
}

internal sealed class TermMonthsSpecification : ISpecification<SimulationHistory>
{
    private readonly int? _termMonths;

    public TermMonthsSpecification(int? termMonths) => _termMonths = termMonths;

    public IQueryable<SimulationHistory> Apply(IQueryable<SimulationHistory> query) =>
        _termMonths is null ? query : query.Where(s => s.TermMonths == _termMonths.Value);
}

internal sealed class AnnualRateMinSpecification : ISpecification<SimulationHistory>
{
    private readonly decimal? _min;

    public AnnualRateMinSpecification(decimal? min) => _min = min;

    public IQueryable<SimulationHistory> Apply(IQueryable<SimulationHistory> query) =>
        _min is null ? query : query.Where(s => s.AnnualRate >= _min.Value);
}

internal sealed class AnnualRateMaxSpecification : ISpecification<SimulationHistory>
{
    private readonly decimal? _max;

    public AnnualRateMaxSpecification(decimal? max) => _max = max;

    public IQueryable<SimulationHistory> Apply(IQueryable<SimulationHistory> query) =>
        _max is null ? query : query.Where(s => s.AnnualRate <= _max.Value);
}

internal sealed class InstallmentTypeSpecification : ISpecification<SimulationHistory>
{
    private readonly string? _installmentType;

    public InstallmentTypeSpecification(string? installmentType) => _installmentType = installmentType;

    public IQueryable<SimulationHistory> Apply(IQueryable<SimulationHistory> query)
    {
        if (string.IsNullOrWhiteSpace(_installmentType))
            return query;

        var normalized = _installmentType.Trim();
        return query.Where(s => EF.Functions.ILike(s.InstallmentType, normalized));
    }
}

internal sealed class CreatedFromSpecification : ISpecification<SimulationHistory>
{
    private readonly DateTime? _from;

    public CreatedFromSpecification(DateTime? from) => _from = from;

    public IQueryable<SimulationHistory> Apply(IQueryable<SimulationHistory> query) =>
        _from is null ? query : query.Where(s => s.CreatedAt >= _from.Value);
}

internal sealed class CreatedToSpecification : ISpecification<SimulationHistory>
{
    private readonly DateTime? _to;

    public CreatedToSpecification(DateTime? to) => _to = to;

    public IQueryable<SimulationHistory> Apply(IQueryable<SimulationHistory> query) =>
        _to is null ? query : query.Where(s => s.CreatedAt <= _to.Value);
}
