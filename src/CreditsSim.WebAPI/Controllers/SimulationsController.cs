using CreditsSim.Application.DTOs;
using CreditsSim.Application.Features.Simulations.Commands;
using CreditsSim.Application.Features.Simulations.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CreditsSim.WebAPI.Controllers;

[ApiController]
[Route("api")]
public class SimulationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SimulationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Simula un crédito personal y devuelve el cronograma de pagos.
    /// </summary>
    [HttpPost("simulation")]
    [ProducesResponseType(typeof(SimulationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] SimulationRequest request, CancellationToken ct)
    {
        var command = new CreateSimulationCommand(
            request.Amount,
            request.TermMonths,
            request.AnnualRate,
            request.InstallmentType
        );

        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Recupera una simulación previa por su ID.
    /// </summary>
    [HttpGet("simulations/{id:guid}")]
    [ProducesResponseType(typeof(SimulationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSimulationQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
