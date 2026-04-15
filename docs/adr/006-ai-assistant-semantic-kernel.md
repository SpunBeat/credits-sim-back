# ADR-006: Asistente AI con Semantic Kernel y Gemini

**Estado:** Aceptado  
**Fecha:** 2026-04-15

## Contexto

Se necesita un asistente conversacional que ayude a los usuarios a crear, consultar, comparar y eliminar simulaciones usando lenguaje natural. El asistente debe operar sobre los mismos datos reales de la base de datos, no inventar cifras.

## Decision

### Stack

- **Microsoft Semantic Kernel 1.54** como orquestador de LLM + function calling.
- **Google Gemini** (modelo `gemini-2.5-flash`) como proveedor de LLM via `Microsoft.SemanticKernel.Connectors.Google`.
- El conector de Google es experimental (`SKEXP0070` suprimido en csproj).

### Plugin: SimulationPlugin

5 tools registradas como `[KernelFunction]` que Gemini puede invocar via function calling:

| Tool | Parametros | Validacion |
|---|---|---|
| `create_simulation` | amount, termMonths, annualRate | amount: 1–10M, term: 1–360, rate: 0–100 |
| `get_simulation` | id (GUID) | Formato GUID valido |
| `list_simulations` | count (1–20, default 5) | Clamped con `Math.Clamp` |
| `compare_simulations` | ids (CSV de GUIDs) | Min 2, max 6 |
| `delete_simulation` | id (GUID) | Formato GUID valido |

### Inyeccion de dependencias scoped

`ISimulationRepository` es scoped (via EF Core DbContext). El plugin se registra como singleton en el Kernel. Para resolver esta incompatibilidad, el plugin inyecta `IServiceScopeFactory` y crea un scope por cada invocacion de tool:

```csharp
using var scope = _scopeFactory.CreateScope();
var repo = scope.ServiceProvider.GetRequiredService<ISimulationRepository>();
```

### Formato de respuesta de los tools

Cada tool devuelve `string` con texto estructurado legible para el LLM (no JSON). El LLM convierte este texto en respuesta natural al usuario. Ejemplo:

```
Simulacion creada exitosamente.
ID: {id}
Monto: $50,000.00
Cuota mensual: $4,613.78
```

### Manejo de errores en tools

Nunca se lanzan excepciones desde el plugin. Todos los errores se devuelven como strings descriptivos:
- ID no encontrado: "No encontre una simulacion con ID {id}."
- GUID invalido: "El ID '{id}' no es un GUID valido."
- Validacion: "No pude crear la simulacion: el monto debe estar entre..."

### System prompt

Instrucciones en español que definen el comportamiento:
- Responder siempre en español, conciso y directo.
- Usar las herramientas para datos reales (nunca inventar cifras).
- Confirmar antes de eliminar si el usuario no fue explicito.
- Redirigir preguntas fuera del dominio financiero.

### Tracking de tools invocadas

Con auto function calling, el Kernel ejecuta las tools internamente. Para reportar cuales se usaron en la respuesta (`toolsUsed`), se implemento `ToolInvocationTracker` — un `IFunctionInvocationFilter` que captura nombres de funciones en un `ConcurrentBag<string>`. Se agrega al Kernel per-request y se remueve despues.

Nota: el conector de Google no agrega `FunctionCallContent` al ChatHistory como OpenAI. El filter es la unica forma confiable de capturar las invocaciones.

### Endpoint

```
POST /api/assistant/chat
```

Request: `{ message, conversationHistory: [{ role, content }] }`
Response: `{ reply, toolsUsed: string[] }`

### Seguridad

- API Key de Gemini via user-secrets (nunca en appsettings commiteados).
- Si la key esta vacia, la app falla en startup con `InvalidOperationException`.
- La key nunca aparece en logs, responses de error, ni ProblemDetails.
- Rate limit separado: 20 req/min por IP (AssistantPolicy).

## Consecuencias

- (+) El asistente opera sobre datos reales — no hay alucinaciones de cifras.
- (+) Function calling automatico — el LLM decide que tool usar segun el mensaje del usuario.
- (+) 5 tools cubren todas las operaciones CRUD + comparacion.
- (+) Texto legible en tools → el LLM genera respuestas naturales sin parsear JSON.
- (-) Conector de Google es alpha — puede haber breaking changes en futuras versiones de SK.
- (-) Cada llamada al chat genera 1-2 requests a Gemini (prompt + function result) — costo por uso.
- (-) Sin streaming — el usuario espera hasta que Gemini complete toda la respuesta.
