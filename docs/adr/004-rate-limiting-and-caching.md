# ADR-004: Rate Limiting y Response Caching

**Estado:** Aceptado  
**Fecha:** 2026-04-14

## Contexto

La API esta expuesta publicamente sin autenticacion. Se necesita proteger contra abuso (requests excesivos) y reducir carga en el listado de simulaciones (endpoint mas frecuente).

## Decision

### Rate limiting nativo de .NET 8

Dos politicas `FixedWindow` particionadas por IP del cliente:

| Politica | Limite | Ventana | Cola | Aplicada en |
|---|---|---|---|---|
| `SimulationsPolicy` | 10 req/min | 1 minuto | 2 (oldest first) | `SimulationsController` |
| `AssistantPolicy` | 20 req/min | 1 minuto | 2 (oldest first) | `AssistantController` |

**Respuesta 429 personalizada:**
- `Content-Type: application/problem+json` (RFC 7807)
- Header `Retry-After` con los segundos hasta que la ventana se reinicie
- Mensaje en español: "Se excedio el limite de peticiones..."

**Exclusion:** `/health` usa `.DisableRateLimiting()`.

### Response caching

`[ResponseCache]` en el endpoint de listado:
- `Duration = 30` segundos
- `Location = ResponseCacheLocation.Client` → `Cache-Control: private, max-age=30`
- `VaryByQueryKeys` incluye todos los parametros (pageSize, sortBy, sortOrder, cursores, filtros)

**Consistencia eventual:** Tras un DELETE, el listado puede mostrar el item eliminado durante hasta 30s hasta que el cache expire en el browser. Documentado en el controller via `<remarks>`.

### Orden del middleware

```
UseExceptionHandler → UseCors → UseResponseCaching → UseRateLimiter → MapControllers
```

`UseResponseCaching` va antes de `UseRateLimiter` para que las respuestas cacheadas no consuman el rate limit del usuario.

## Consecuencias

- (+) Proteccion inmediata contra abuso sin infraestructura externa.
- (+) Cache client-side reduce queries a PostgreSQL en el endpoint mas usado.
- (+) El assistant tiene limite separado (20 vs 10) porque las llamadas a Gemini son costosas pero el usuario necesita interactuar fluidamente.
- (-) Rate limiting por IP no distingue usuarios autenticados. Si se agrega auth, considerar politicas por usuario.
- (-) Cache de 30s introduce consistencia eventual. Aceptable para un historial que cambia poco.
