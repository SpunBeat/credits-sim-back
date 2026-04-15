using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Concurrent;

namespace CreditsSim.WebAPI.Controllers;

/// <summary>
/// Asistente financiero AI para simulaciones de crédito (Gemini via Semantic Kernel).
/// </summary>
[ApiController]
[Route("api/assistant")]
[Produces("application/json")]
[EnableRateLimiting("AssistantPolicy")]
public class AssistantController : ControllerBase
{
    private readonly Kernel _kernel;

    private const string SystemPrompt = """
        Eres un asistente financiero especializado en créditos personales dentro de un simulador de préstamos. Tu función es ayudar al usuario a crear simulaciones, comparar escenarios y entender sus resultados.

        Reglas:
        - Responde siempre en español, de forma concisa y directa
        - Cuando el usuario pida simular, usa create_simulation con los datos exactos
        - Cuando pida comparar, usa compare_simulations con los IDs relevantes
        - Antes de eliminar, confirma con el usuario si no lo ha hecho explícitamente
        - No inventes datos financieros - usa siempre las herramientas disponibles
        - Si el usuario pregunta algo fuera de tu dominio, redirige amablemente
        """;

    public AssistantController(Kernel kernel)
    {
        _kernel = kernel;
    }

    /// <summary>
    /// Envía un mensaje al asistente financiero y recibe una respuesta con posibles invocaciones de herramientas.
    /// </summary>
    /// <param name="request">Mensaje del usuario y contexto de la conversación.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Respuesta del asistente con las herramientas utilizadas.</returns>
    /// <response code="200">Respuesta del asistente.</response>
    /// <response code="503">El servicio de AI no está disponible.</response>
    [HttpPost("chat")]
    [SwaggerOperation(OperationId = "chatWithAssistant")]
    [ProducesResponseType(typeof(AssistantChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Chat([FromBody] AssistantChatRequest request, CancellationToken ct)
    {
        try
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            // Build history: system prompt + conversation context + current message
            var history = new ChatHistory();
            history.AddSystemMessage(SystemPrompt);

            foreach (var msg in request.ConversationHistory)
            {
                if (string.Equals(msg.Role, "user", StringComparison.OrdinalIgnoreCase))
                    history.AddUserMessage(msg.Content);
                else if (string.Equals(msg.Role, "assistant", StringComparison.OrdinalIgnoreCase))
                    history.AddAssistantMessage(msg.Content);
            }

            history.AddUserMessage(request.Message);

            // Track tool invocations via a filter
            var tracker = new ToolInvocationTracker();
            _kernel.FunctionInvocationFilters.Add(tracker);

            var settings = new GeminiPromptExecutionSettings
            {
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
            };

            try
            {
                var response = await chatService.GetChatMessageContentAsync(
                    history, settings, _kernel, ct);

                return Ok(new AssistantChatResponse(
                    Reply: response.Content ?? string.Empty,
                    ToolsUsed: tracker.InvokedFunctions.ToList()
                ));
            }
            finally
            {
                _kernel.FunctionInvocationFilters.Remove(tracker);
            }
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
                Title = "Service Unavailable",
                Status = 503,
                Detail = "No se pudo conectar con el servicio de AI. Intenta de nuevo en unos momentos.",
            });
        }
    }

    /// <summary>
    /// Captures function names as they are invoked by the auto function calling loop.
    /// Added to Kernel.FunctionInvocationFilters per-request, removed after completion.
    /// </summary>
    private sealed class ToolInvocationTracker : IFunctionInvocationFilter
    {
        private readonly ConcurrentBag<string> _names = new();
        public IReadOnlyCollection<string> InvokedFunctions => _names.Distinct().ToList();

        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            _names.Add(context.Function.Name);
            await next(context);
        }
    }
}

// ── DTOs ──────────────────────────────────────────────────────────

/// <summary>Mensaje en el historial de conversación.</summary>
public record ConversationMessage
{
    /// <summary>Rol: "user" o "assistant".</summary>
    public string Role { get; init; } = string.Empty;
    /// <summary>Contenido del mensaje.</summary>
    public string Content { get; init; } = string.Empty;
}

/// <summary>Request del chat con el asistente.</summary>
public record AssistantChatRequest
{
    /// <summary>Mensaje actual del usuario.</summary>
    public string Message { get; init; } = string.Empty;
    /// <summary>Historial de mensajes previos de la conversación.</summary>
    public List<ConversationMessage> ConversationHistory { get; init; } = [];
}

/// <summary>Respuesta del asistente.</summary>
public record AssistantChatResponse(string Reply, List<string> ToolsUsed);
