using CreditsSim.Application.DTOs;
using CreditsSim.Domain.Interfaces;
using CreditsSim.Domain.Query;
using MediatR;

namespace CreditsSim.Application.Features.Simulations.Queries;

public record ListSimulationsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string SortOrder = "desc",
    decimal? AmountMin = null,
    decimal? AmountMax = null,
    int? TermMonths = null,
    decimal? AnnualRateMin = null,
    decimal? AnnualRateMax = null,
    string? InstallmentType = null,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null
) : IRequest<PagedResponse<SimulationSummary>>;

public class ListSimulationsHandler : IRequestHandler<ListSimulationsQuery, PagedResponse<SimulationSummary>>
{
    private readonly ISimulationRepository _repository;

    public ListSimulationsHandler(ISimulationRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<SimulationSummary>> Handle(ListSimulationsQuery request, CancellationToken ct)
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

        var (entities, totalCount) = await _repository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            ascending,
            filter,
            ct);

        var items = entities.Select(e => new SimulationSummary
        {
            Id = e.Id,
            Amount = e.Amount,
            TermMonths = e.TermMonths,
            AnnualRate = e.AnnualRate,
            InstallmentType = e.InstallmentType,
            CreatedAt = e.CreatedAt
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResponse<SimulationSummary>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = totalPages
        };
    }
}
