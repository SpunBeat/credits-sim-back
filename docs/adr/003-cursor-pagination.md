# ADR-003: Cursor-Based Pagination con Headers HTTP

**Estado:** Aceptado  
**Fecha:** 2026-04-14

## Contexto

El listado de simulaciones necesita paginacion eficiente. La paginacion por offset (`SKIP/TAKE`) degrada exponencialmente con datasets grandes (pagina 10,000 requiere leer y descartar 99,990 filas). Ademas, los metadatos de paginacion en el body del response acoplan al frontend con la estructura del DTO.

## Decision

### Keyset pagination (cursor-based)

Reemplazar `pageNumber` por dos cursores: `cursorCreatedAt` (DateTime) y `cursorId` (Guid).

**Query SQL generado:**

```sql
-- Descending (default)
WHERE (created_at < @cursor) OR (created_at = @cursor AND id < @cursorId)
ORDER BY created_at DESC, id DESC
LIMIT @pageSize + 1
```

El `+1` extra detecta si hay pagina siguiente sin ejecutar `COUNT(*)`:
- Si devuelve `pageSize + 1` filas → `hasNextPage = true`, se descarta la ultima.
- Si devuelve `≤ pageSize` filas → `hasNextPage = false`.

### Indice compuesto

```sql
CREATE INDEX ix_simulation_history_created_at_id
ON simulation_history (created_at DESC, id DESC);
```

Cubre tanto el `ORDER BY` como el `WHERE` del cursor, permitiendo index-only scan.

### Metadatos en headers HTTP

Un `IAsyncResultFilter` (`PaginationHeaderFilter`) detecta automaticamente si el action result es `ICursorPagedResult` y:
1. Extrae metadatos a headers.
2. Reemplaza el body con solo el array de items.

| Header | Tipo | Cuando se envia |
|---|---|---|
| `X-Total-Count` | int | Solo en primera pagina (sin cursor) |
| `X-Page-Size` | int | Siempre |
| `X-Has-Next-Page` | bool | Siempre |
| `X-Next-Cursor-CreatedAt` | ISO 8601 | Cuando hay items |
| `X-Next-Cursor-Id` | UUID | Cuando hay items |

CORS expone estos headers via `WithExposedHeaders()`.

### Total count opcional

`COUNT(*)` solo se ejecuta en la primera pagina (cuando no hay cursor). En paginas subsecuentes se omite para evitar la query costosa. El frontend muestra el total solo en el subtitle del primer load.

### Ordenamiento dinamico

El parametro `sortBy` acepta 5 columnas: `createdAt` (default), `amount`, `termMonths`, `annualRate`, `installmentType`. La columna seleccionada se usa como sort primario, con `(createdAt, Id)` como tiebreaker para que el cursor funcione independientemente de la columna elegida.

Validacion via whitelist en `ListSimulationsValidator`.

## Consecuencias

- (+) Rendimiento constante O(1) independiente de la profundidad de pagina.
- (+) Body limpio (array puro) — compatible con cualquier cliente.
- (+) El filter es transparente — cualquier endpoint que devuelva `ICursorPagedResult` hereda los headers sin codigo extra.
- (+) `VaryByQueryKeys` en `[ResponseCache]` incluye los cursores para cache granular.
- (-) No permite saltar a paginas aleatorias — el frontend usa patron "Cargar mas".
- (-) Cambiar el sort resetea la navegacion al inicio.
