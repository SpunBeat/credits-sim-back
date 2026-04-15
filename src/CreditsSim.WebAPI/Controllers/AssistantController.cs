using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Swashbuckle.AspNetCore.Annotations;

namespace CreditsSim.WebAPI.Controllers;

/// <summary>
/// Endpoint de verificación del asistente AI (Gemini via Semantic Kernel).
/// </summary>
[ApiController]
[Route("api/assistant")]
[Produces("application/json")]
public class AssistantController : ControllerBase
{
    private readonly Kernel _kernel;

    public AssistantController(Kernel kernel)
    {
        _kernel = kernel;
    }

    /// <summary>
    /// Verifica que la conexión con Gemini esté funcional.
    /// </summary>
    /// <response code="200">Gemini respondió correctamente.</response>
    /// <response code="503">No se pudo conectar con Gemini.</response>
    [HttpGet("ping")]
    [SwaggerOperation(OperationId = "pingAssistant")]
    [ProducesResponseType(typeof(AssistantPingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ping(CancellationToken ct)
    {
        try
        {
            var chat = _kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddUserMessage("Responde exactamente: Gemini funcionando.");

            var response = await chat.GetChatMessageContentAsync(history, cancellationToken: ct);
            var text = response.Content ?? string.Empty;

            return Ok(new AssistantPingResponse(text));
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
                Title = "Service Unavailable",
                Status = 503,
                Detail = "No se pudo conectar con el servicio de AI. Verifica la configuración.",
            });
        }
    }
}

/// <summary>Respuesta del ping al asistente.</summary>
public record AssistantPingResponse(string Message);
