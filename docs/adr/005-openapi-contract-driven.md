# ADR-005: Contract-Driven Development con OpenAPI

**Estado:** Aceptado  
**Fecha:** 2026-04-09

## Contexto

El frontend Angular consume la API via un SDK generado automaticamente. Las inconsistencias entre el contrato OpenAPI y los DTOs de C# causan bugs en el frontend (propiedades opcionales que deberian ser requeridas, nombres de metodos ilegibles, errores de validacion sin tipado).

## Decision

### Tipado estricto en OpenAPI

**RequireNonNullableSchemaFilter:** Un `ISchemaFilter` que usa `NullabilityInfoContext` de .NET 8 para inspeccionar cada propiedad de los DTOs. Las propiedades non-nullable de C# se marcan como `required` en el schema OpenAPI, eliminando los `?` innecesarios en las interfaces TypeScript generadas.

**Records con propiedades explicitas:** Los DTOs usan `record` con `{ get; init; }` (no positional records) para que los XML comments se generen correctamente por propiedad.

### OperationId semanticos

Cada action del controller tiene `[SwaggerOperation(OperationId = "...")]`:

| OperationId | Metodo TypeScript generado |
|---|---|
| `createSimulation` | `createSimulation()` |
| `getSimulationById` | `getSimulationById()` |
| `listSimulations` | `listSimulations()` |
| `deleteSimulation` | `deleteSimulation()` |
| `chatWithAssistant` | `chatWithAssistant()` |

### JSON serialization

Configurado en `Program.cs`:
- `PropertyNamingPolicy = CamelCase` — consistente con TypeScript
- `JsonStringEnumConverter` — enums como strings, no enteros
- `WhenWritingNull` — no envia campos null

### Errores tipados

**`ValidationErrorResponse`:** DTO explicito para errores 400 con `errors: ValidationFieldError[]`. El `GlobalExceptionHandler` serializa este tipo exacto (no `ProblemDetails` con extensions anonimas). El frontend sabe exactamente la forma del JSON de error.

### XML documentation

`<GenerateDocumentationFile>` habilitado en WebAPI y Application. Swagger incluye los XML comments de ambos assemblies via `IncludeXmlComments()`. Las descripciones llegan al frontend como JSDoc en las interfaces generadas.

### Schema IDs para genericos

`CustomSchemaIds` resuelve `PagedResponse<SimulationSummary>` → `PagedResponseSimulationSummary` para evitar colisiones de nombres en el generador.

## Consecuencias

- (+) El frontend regenera el SDK y el compilador de TypeScript detecta breaking changes automaticamente.
- (+) Propiedades `required` eliminan null coalescing innecesario en los componentes Angular.
- (+) Nombres de metodos legibles en vez de `apiSimulationPost`.
- (+) Errores de validacion completamente tipados end-to-end.
- (-) Requiere disciplina: cada cambio de DTO necesita regenerar el SDK en el frontend.
