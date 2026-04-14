using CreditsSim.Application.DTOs;
using CreditsSim.Application.Features.Simulations.Commands;
using CreditsSim.Application.Features.Simulations.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace CreditsSim.WebAPI.Controllers;

/// <summary>
/// Endpoints para simulación de créditos personales.
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
[EnableRateLimiting("SimulationsPolicy")]
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
    [SwaggerOperation(OperationId = "createSimulation")]
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
    [SwaggerOperation(OperationId = "getSimulationById")]
    [ProducesResponseType(typeof(SimulationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSimulationQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Lista el historial de simulaciones con paginación basada en cursor y filtros opcionales.
    /// </summary>
    /// <param name="pageSize">Cantidad de elementos por página (1-100). Por defecto: 10.</param>
    /// <param name="sortOrder">Ordenamiento por fecha de creación: "desc" o "asc". Por defecto: desc.</param>
    /// <param name="cursorCreatedAt">Timestamp del último elemento de la página anterior (cursor).</param>
    /// <param name="cursorId">ID del último elemento de la página anterior (desempate del cursor).</param>
    /// <param name="amountMin">Monto mínimo (inclusive).</param>
    /// <param name="amountMax">Monto máximo (inclusive).</param>
    /// <param name="termMonths">Plazo exacto en meses.</param>
    /// <param name="annualRateMin">Tasa anual mínima (%), inclusive.</param>
    /// <param name="annualRateMax">Tasa anual máxima (%), inclusive.</param>
    /// <param name="installmentType">Tipo de cuota (p. ej. FIXED).</param>
    /// <param name="createdFrom">Creado en o después de esta fecha/hora (UTC).</param>
    /// <param name="createdTo">Creado en o antes de esta fecha/hora (UTC).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista paginada de simulaciones con cursores para la siguiente página.</returns>
    /// <response code="200">Listado con cursores en headers.</response>
    /// <response code="400">Parámetros inválidos.</response>
    [HttpGet("simulations")]
    [SwaggerOperation(OperationId = "listSimulations")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client,
        VaryByQueryKeys = ["pageSize", "sortOrder", "cursorCreatedAt", "cursorId",
            "amountMin", "amountMax", "termMonths",
            "annualRateMin", "annualRateMax", "installmentType",
            "createdFrom", "createdTo"])]
    [ProducesResponseType(typeof(List<SimulationSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortOrder = "desc",
        [FromQuery] DateTime? cursorCreatedAt = null,
        [FromQuery] Guid? cursorId = null,
        [FromQuery] decimal? amountMin = null,
        [FromQuery] decimal? amountMax = null,
        [FromQuery] int? termMonths = null,
        [FromQuery] decimal? annualRateMin = null,
        [FromQuery] decimal? annualRateMax = null,
        [FromQuery] string? installmentType = null,
        [FromQuery] DateTime? createdFrom = null,
        [FromQuery] DateTime? createdTo = null,
        CancellationToken ct = default)
    {
        var query = new ListSimulationsQuery(
            pageSize,
            sortOrder,
            cursorCreatedAt,
            cursorId,
            amountMin,
            amountMax,
            termMonths,
            annualRateMin,
            annualRateMax,
            string.IsNullOrWhiteSpace(installmentType) ? null : installmentType.Trim(),
            createdFrom,
            createdTo);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }
}
