# ADR-009: Soporte para Sistema de Amortizacion Aleman

**Estado:** Aceptado  
**Fecha:** 2026-04-15  
**Extiende:** ADR-002 (Motor de Amortizacion — Sistema Frances)

## Impacta

- **ADR-002:** Se generaliza el motor de amortizacion a una interfaz (`IAmortizationCalculator`) con dos implementaciones.
- **ADR-005 (OpenAPI y serializacion):** `installmentType` ahora se expone como enum tipado (`FIXED | GERMAN`) en el schema OpenAPI. El SDK del frontend debe regenerarse tras merge.
- **ADR-006 (Semantic Kernel Assistant):** Tool `create_simulation` recibe parametro `installmentType`. Tools `get_simulation` y `compare_simulations` rotulan cuotas como "Cuota inicial / Cuota final" cuando el tipo es GERMAN.
- **ADR-007 (Validaciones):** `CreateSimulationValidator` valida el enum via `IsInEnum()`; `ListSimulationsValidator` acepta "FIXED" o "GERMAN" en el query-string con `Enum.TryParse` (ignoreCase).

## No impacta

- ADR-003: Paginacion por cursores.
- ADR-004: Rate limiting.
- ADR-008: Eliminacion y ordenamiento dinamico.

## Contexto

ADR-002 construyo el motor asumiendo exclusivamente **Sistema Frances** (cuota fija). Los clientes preferien con frecuencia el **Sistema Aleman** (capital constante, cuotas decrecientes) por el menor costo financiero total: la porcion de interes se reduce mes a mes porque se aplica sobre un saldo decreciente.

Se necesita que el simulador ofrezca ambos sistemas sin duplicar la infraestructura de persistencia, validacion ni serializacion.

## Decision

### Patron arquitectonico: Strategy + Factory

- `IAmortizationCalculator` define la interfaz comun (`SupportedType`, `Calculate`).
- `AmortizationCalculatorBase` centraliza `InsuranceRateMonthly` (evita duplicacion de la constante entre sistemas).
- `FrenchAmortizationCalculator` y `GermanAmortizationCalculator` heredan y encapsulan la logica de cada sistema.
- `AmortizationCalculatorFactory` resuelve el calculador correcto segun el `InstallmentType` recibido.

La entidad `SimulationHistory` persiste `InstallmentType` como string (`varchar(20)`) por compatibilidad hacia atras — la conversion a enum se hace en los handlers (boundary Application).

### Tipo fuertemente tipado

`InstallmentType` es un enum C# (`FIXED`, `GERMAN`) expuesto como string en JSON via `JsonStringEnumConverter` (ya configurado globalmente en `Program.cs`). El schema OpenAPI refleja `"enum": ["FIXED", "GERMAN"]` automaticamente.

Consecuencia: valores invalidos en JSON (ej. `"german"`, `null`, desconocidos) son rechazados por el deserializer con 400 antes de llegar al validator. El factory y los validators dejaron de hacer comparacion de strings case-insensitive.

### Reglas del Sistema Aleman

- **Capital constante:** `P_m = round(P / n, 2)`
- **Interes variable:** `I_m = Saldo × r_mensual` (sobre saldo al inicio del periodo)
- **Cuota total:** `C_m = P_m + I_m + Seguro_m` (decrece periodo a periodo)
- **Seguro:** `0.065%` mensual sobre saldo (mismo criterio que Frances — heredado de `AmortizationCalculatorBase`)
- **Ultimo periodo:** `P_m = saldo pendiente` para absorber el drift de redondeo. El saldo final siempre cierra en 0.

### Casos borde

| Caso | Comportamiento |
|---|---|
| Plazo = 1 | Cuota unica = monto + interes + seguro; saldo final = 0. |
| Tasa = 0% | Interes = 0 en todos los periodos; la cuota decrece solo por el seguro sobre saldo decreciente. |
| Monto no divisible entre plazo | El capital redondeado a 2 decimales genera drift; el ultimo periodo ajusta `principal = saldo` para cerrar en 0. |

### Validaciones

Las reglas de negocio (monto, plazo, tasa) son identicas para ambos sistemas — el ADR no diferencia limites porque ambos sistemas operan sobre el mismo rango economico soportado por la plataforma.

## Alternativas consideradas

- **Switch en el handler:** Mas simple, menos clases.  
  Descartado: viola Open/Closed Principle. Cada nuevo sistema obligaria a tocar el handler.

- **`Dictionary<string, Func<...>>` estatico:** Menos overhead de DI.  
  Descartado: dificulta la inyeccion de dependencias si mas adelante los calculadores necesitan configuracion (`IOptions<AmortizationSettings>`), logging o telemetria.

- **Keyed services (.NET 8):** Elimina el factory completamente.  
  Descartado por ahora: el factory ya existe y funciona. Registrado como deuda tecnica (ver `DependencyInjection.cs` — TODO keyed services).

## Consecuencias para el frontend

- **Regenerar SDK** tras merge:
  ```bash
  curl http://localhost:5087/swagger/v1/swagger.json -o swagger.json
  npm run generate:api:file
  ```
- **`SimulatorComponent`, `HistoryComponent`, `ComparatorComponent`** deben mostrar **"Cuota inicial / Cuota final"** cuando `installmentType === 'GERMAN'`, no "Cuota mensual" (esa metrica es inexacta para cuotas decrecientes).
- Opcionalmente: grafico de evolucion de cuota para GERMAN (ayuda al usuario a visualizar que la cuota baja con el tiempo).
- Abrir ticket de frontend como follow-up.

## Consecuencias

- (+) Dos esquemas de amortizacion mas populares soportados sin duplicar infraestructura.
- (+) Strategy + Factory permite agregar sistemas futuros (Americano, mixto) sin modificar codigo existente (Open/Closed).
- (+) Enum C# + `JsonStringEnumConverter` centraliza valores validos: el contrato OpenAPI refleja automaticamente los sistemas soportados.
- (+) `AmortizationCalculatorBase` elimina la duplicacion de `InsuranceRateMonthly` que habria crecido a 3+ copias al sumar sistemas.
- (-) `AmortizationCalculatorFactory` lanza `NotSupportedException` si el enum esta fuera de rango; se mapea a 400 en `GlobalExceptionHandler`.
- (-) El seguro sigue cableado en constante (`InsuranceRateMonthly`). TODO: migrar a `IOptions<AmortizationSettings>`.
- (-) `SimulationHistory.InstallmentType` sigue siendo `string` en la BD. Cambio futuro requeriria migracion.

## Tests

Se agregan tests en `CreditsSim.Tests/Calculators/` cubriendo invariantes matematicas (saldo final = 0, Σ capital = monto, cuotas decrecientes), cronograma conocido fila a fila para German, y resolucion del factory para FIXED/GERMAN/invalido. Los tests son parte del contrato de esta decision arquitectonica.
