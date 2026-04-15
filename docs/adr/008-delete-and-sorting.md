# ADR-008: Eliminacion Fisica y Ordenamiento Dinamico

**Estado:** Aceptado  
**Fecha:** 2026-04-14

## Contexto

El historial de simulaciones necesita permitir eliminar registros y ordenar resultados por diferentes columnas, manteniendo la compatibilidad con cursor-based pagination.

## Decision

### Eliminacion fisica

`DELETE /api/simulations/{id}` ejecuta borrado fisico usando `ExecuteDeleteAsync` de EF Core:

```csharp
var rows = await _context.SimulationHistories
    .Where(s => s.Id == id)
    .ExecuteDeleteAsync(ct);
return rows > 0;
```

**Ventajas de `ExecuteDeleteAsync`:**
- Ejecuta `DELETE FROM ... WHERE id = @id` directo en SQL.
- No carga la entidad en memoria — no hay riesgo de conflicto de concurrencia.
- Operacion atomica.

**Respuestas:**
- 204 No Content si se elimino.
- 404 Not Found si el ID no existia.

No se implementa soft delete. Las simulaciones eliminadas desaparecen permanentemente. Justificacion: son calculos efimeros, no transacciones financieras reales.

### Ordenamiento dinamico

`sortBy` acepta 5 columnas, mapeadas via diccionario de expresiones:

```csharp
private static readonly Dictionary<string, Expression<Func<SimulationHistory, object>>> SortColumns = new()
{
    ["createdAt"] = s => s.CreatedAt,
    ["amount"] = s => s.Amount,
    ["termMonths"] = s => s.TermMonths,
    ["annualRate"] = s => s.AnnualRate,
    ["installmentType"] = s => s.InstallmentType,
};
```

La columna elegida se usa como sort primario. El tiebreaker `(CreatedAt, Id)` se aplica siempre como sort secundario para que los cursores funcionen independientemente de la columna primaria.

**Validacion:** whitelist en `ListSimulationsValidator` — `sortBy` invalido retorna 400.

### Interaccion con cache

El listado tiene `Cache-Control: private, max-age=30`. Tras un DELETE:
- La base de datos refleja el cambio inmediatamente.
- Otros tabs del mismo usuario pueden mostrar el item durante hasta 30s.
- No hay invalidacion activa de cache — consistencia eventual aceptable.

`VaryByQueryKeys` incluye `sortBy` para que cada combinacion de sort tenga su propia entrada de cache.

## Consecuencias

- (+) `ExecuteDeleteAsync` es la forma mas eficiente de borrar en EF Core 8.
- (+) Sort dinamico via diccionario de expresiones — agregar columnas es trivial.
- (+) Tiebreaker garantiza estabilidad del cursor con cualquier sort.
- (-) Sin soft delete ni audit trail. Si se necesita historial de eliminaciones, agregar tabla de auditoria.
- (-) 30s de consistencia eventual tras DELETE. Aceptable para el caso de uso.
