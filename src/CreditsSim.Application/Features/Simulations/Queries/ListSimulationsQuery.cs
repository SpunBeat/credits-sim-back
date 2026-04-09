using CreditsSim.Application.DTOs;
using CreditsSim.Domain.Interfaces;
using MediatR;

namespace CreditsSim.Application.Features.Simulations.Queries;

public record ListSimulationsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string SortOrder = "desc"
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

        var (entities, totalCount) = await _repository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            ascending,
            ct);

        var items = entities.Select(e => new SimulationSummary(
            e.Id,
            e.Amount,
            e.TermMonths,
            e.AnnualRate,
            e.InstallmentType,
            e.CreatedAt
        )).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResponse<SimulationSummary>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize,
            totalPages
        );
    }
}
