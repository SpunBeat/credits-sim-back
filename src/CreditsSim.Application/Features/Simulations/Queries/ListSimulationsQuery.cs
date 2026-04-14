using CreditsSim.Application.DTOs;
using CreditsSim.Domain.Interfaces;
using CreditsSim.Domain.Query;
using MediatR;

namespace CreditsSim.Application.Features.Simulations.Queries;

public record ListSimulationsQuery(
    int PageSize = 10,
    string SortOrder = "desc",
    DateTime? CursorCreatedAt = null,
    Guid? CursorId = null,
    decimal? AmountMin = null,
    decimal? AmountMax = null,
    int? TermMonths = null,
    decimal? AnnualRateMin = null,
    decimal? AnnualRateMax = null,
    string? InstallmentType = null,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null
) : IRequest<CursorPagedResponse<SimulationSummary>>;

public class ListSimulationsHandler : IRequestHandler<ListSimulationsQuery, CursorPagedResponse<SimulationSummary>>
{
    private readonly ISimulationRepository _repository;

    public ListSimulationsHandler(ISimulationRepository repository)
    {
        _repository = repository;
    }

    public async Task<CursorPagedResponse<SimulationSummary>> Handle(ListSimulationsQuery request, CancellationToken ct)
    {
        var ascending = string.Equals(request.SortOrder, "asc", StringComparison.OrdinalIgnoreCase);

        SimulationListFilter? filter = null;
        if (request.AmountMin.HasValue || request.AmountMax.HasValue || request.TermMonths.HasValue
            || request.AnnualRateMin.HasValue || request.AnnualRateMax.HasValue
            || !string.IsNullOrWhiteSpace(request.InstallmentType)
            || request.CreatedFrom.HasValue || request.CreatedTo.HasValue)
        {
            filter = new SimulationListFilter(
                request.AmountMin,
                request.AmountMax,
                request.TermMonths,
                request.AnnualRateMin,
                request.AnnualRateMax,
                request.InstallmentType,
                request.CreatedFrom,
                request.CreatedTo);
        }

        var (entities, hasNextPage) = await _repository.GetCursorPagedAsync(
            request.PageSize,
            ascending,
            request.CursorCreatedAt,
            request.CursorId,
            filter,
            ct);

        // Optional total count (only on first page — no cursor = first request)
        int? totalCount = null;
        if (!request.CursorCreatedAt.HasValue)
        {
            totalCount = await _repository.CountAsync(filter, ct);
        }

        var items = entities.Select(e => new SimulationSummary
        {
            Id = e.Id,
            Amount = e.Amount,
            TermMonths = e.TermMonths,
            AnnualRate = e.AnnualRate,
            InstallmentType = e.InstallmentType,
            CreatedAt = e.CreatedAt
        }).ToList();

        var lastItem = entities.LastOrDefault();

        return new CursorPagedResponse<SimulationSummary>
        {
            Items = items,
            PageSize = request.PageSize,
            HasNextPage = hasNextPage,
            NextCursorCreatedAt = lastItem?.CreatedAt,
            NextCursorId = lastItem?.Id,
            TotalCount = totalCount,
        };
    }
}
