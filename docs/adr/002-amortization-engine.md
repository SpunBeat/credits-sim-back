# ADR-002: Motor de Amortizacion — Sistema Frances

**Estado:** Aceptado  
**Fecha:** 2026-04-08

## Contexto

El simulador necesita calcular cronogramas de pago para creditos personales. Se requiere definir el sistema de amortizacion, la formula, el manejo de seguro de desgravamen y las reglas de redondeo.

## Decision

### Sistema de amortizacion

**Sistema Frances** (cuotas constantes): la cuota total es fija durante toda la vida del credito. La porcion de capital crece y la de interes decrece mes a mes.

### Formula

```
Cuota = P × [r(1+r)^n] / [(1+r)^n - 1]
```

Donde:
- P = monto del prestamo
- r = tasa mensual (tasa anual / 12 / 100)
- n = plazo en meses

### Seguro de desgravamen

- Tasa: **0.065% mensual** sobre saldo pendiente (`InsuranceRateMonthly = 0.00065m`).
- Se calcula sobre el saldo al inicio de cada cuota.
- Se suma a la cuota fija (capital + interes) como componente separado.
- Aparece como campo independiente en `ScheduleRow.Insurance`.

### Casos borde

| Caso | Manejo |
|---|---|
| Tasa 0% | Cuota = monto / plazo (division directa, sin formula de anualidad) |
| Ultimo mes | Capital se ajusta al saldo restante para evitar errores de redondeo acumulado |
| Saldo negativo | Se clampea a 0 (`if (balance < 0) balance = 0`) |
| Redondeo | Todos los valores monetarios redondeados a 2 decimales con `Math.Round` |

### Validaciones (FluentValidation)

| Campo | Regla | Mensaje |
|---|---|---|
| Amount | > 0 y ≤ 10,000,000 | "El monto debe ser mayor a 0" / "no puede exceder 10,000,000" |
| TermMonths | 1 a 360 | "El plazo debe estar entre 1 y 360 meses" |
| AnnualRate | 0 a 100 | "La tasa anual no puede ser negativa" / "no puede exceder 100%" |
| InstallmentType | Solo "FIXED" | "Tipo de cuota no soportado. Use: FIXED" |

### Almacenamiento

El cronograma completo se serializa como JSON y se almacena en la columna `schedule_json` (tipo `jsonb` en PostgreSQL). Esto permite:
- Consultar el resultado sin recalcular.
- Auditar los valores exactos que se mostraron al usuario.
- Buscar dentro del JSON con operadores nativos de PostgreSQL si se necesita en el futuro.

## Consecuencias

- (+) Sistema Frances es el estandar en banca latinoamericana — los usuarios lo reconocen.
- (+) Seguro como componente separado permite mostrarlo desglosado en la UI.
- (+) JSON persistido evita discrepancias entre calculo original y consultas posteriores.
- (-) Solo soporta un tipo de cuota (FIXED). Agregar sistema aleman requerira un nuevo calculador.
- (-) El seguro esta hardcodeado. Si cambia la tasa, requiere deploy.
