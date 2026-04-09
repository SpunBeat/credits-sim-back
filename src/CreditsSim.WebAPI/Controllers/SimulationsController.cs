using CreditsSim.Application.DTOs;
using CreditsSim.Application.Features.Simulations.Commands;
using CreditsSim.Application.Features.Simulations.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CreditsSim.WebAPI.Controllers;

/// <summary>
/// Endpoints para simulación de créditos personales.
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
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
    /// <param name="request">Datos del crédito a simular.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La simulación creada con su cronograma completo.</returns>
    /// <response code="201">Simulación creada exitosamente.</response>
    /// <response code="400">Datos de entrada inválidos (ver detalle de errores).</response>
    [HttpPost("simulation")]
    [ProducesResponseType(typeof(SimulationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
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
    /// <param name="id">Identificador único (GUID) de la simulación.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La simulación con su cronograma completo.</returns>
    /// <response code="200">Simulación encontrada.</response>
    /// <response code="404">No se encontró una simulación con el ID proporcionado.</response>
    [HttpGet("simulations/{id:guid}")]
    [ProducesResponseType(typeof(SimulationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSimulationQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Lista el historial de simulaciones con paginación.
    /// </summary>
    /// <param name="pageNumber">Número de página (1-based). Por defecto: 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página (1-100). Por defecto: 10.</param>
    /// <param name="sortOrder">Ordenamiento por fecha de creación: "desc" (más recientes primero) o "asc". Por defecto: desc.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista paginada de simulaciones.</returns>
    /// <response code="200">Listado paginado.</response>
    /// <response code="400">Parámetros de paginación inválidos.</response>
    [HttpGet("simulations")]
    [ProducesResponseType(typeof(PagedResponse<SimulationSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortOrder = "desc",
        CancellationToken ct = default)
    {
        var query = new ListSimulationsQuery(pageNumber, pageSize, sortOrder);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }
}
