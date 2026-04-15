# ADR-007: Validacion y Manejo Global de Errores

**Estado:** Aceptado  
**Fecha:** 2026-04-09

## Contexto

Los errores de validacion y las excepciones no manejadas deben tener un formato consistente que el frontend pueda parsear de forma predecible. Los mensajes deben ser utiles para el usuario final (en español) y compatibles con el SDK generado.

## Decision

### FluentValidation con Pipeline Behavior

Las validaciones se ejecutan automaticamente antes de cada handler de MediatR via `ValidationBehavior<TRequest, TResponse>`:

1. El controller envia el command/query a MediatR.
2. El behavior busca todos los `IValidator<TRequest>` registrados.
3. Si hay errores, lanza `ValidationException` antes de que el handler se ejecute.
4. El `GlobalExceptionHandler` la intercepta.

### Reglas de validacion implementadas

**CreateSimulationValidator:**

| Campo | Regla |
|---|---|
| Amount | > 0, ≤ 10,000,000 |
| TermMonths | 1–360 inclusive |
| AnnualRate | 0–100% |
| InstallmentType | Solo "FIXED" |

**ListSimulationsValidator:**

| Campo | Regla |
|---|---|
| PageSize | 1–100 |
| SortBy | Whitelist: createdAt, amount, termMonths, annualRate, installmentType |
| SortOrder | "asc" o "desc" |
| CursorCreatedAt + CursorId | Ambos requeridos juntos (o ninguno) |
| AmountMin/Max | Min ≤ Max (condicional) |
| AnnualRateMin/Max | Min ≤ Max (condicional) |
| TermMonths (filtro) | 1–360 (condicional) |
| InstallmentType (filtro) | Vacio o "FIXED" |
| CreatedFrom/To | From ≤ To (condicional) |

### GlobalExceptionHandler

Implementa `IExceptionHandler` de .NET 8. Mapeo de excepciones:

| Excepcion | Status | Response |
|---|---|---|
| `ValidationException` | 400 | `ValidationErrorResponse` con `errors[]` tipado |
| Cualquier otra | 500 | `ProblemDetails` generico (no expone stack trace) |

### ValidationErrorResponse (DTO tipado)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": [
    { "propertyName": "Amount", "errorMessage": "El monto debe ser mayor a 0." }
  ]
}
```

El campo `errors` tiene tipo `List<ValidationFieldError>` (no anonymous object). Esto permite que el SDK genere una interfaz TypeScript exacta que el frontend parsea sin casting.

### Serialization

`application/problem+json` con `CamelCase` para consistencia con el resto de la API.

## Consecuencias

- (+) Validacion ejecutada antes del handler — la logica de negocio nunca recibe datos invalidos.
- (+) `ValidationErrorResponse` es un contrato tipado — el frontend sabe exactamente como parsear errores.
- (+) Mensajes en español — el frontend puede mostrarlos directamente al usuario.
- (+) Stack traces nunca se exponen en produccion (el 500 generico dice "An unexpected error occurred").
- (-) Las validaciones estan duplicadas en el plugin (SimulationPlugin valida manualmente porque no pasa por MediatR).
