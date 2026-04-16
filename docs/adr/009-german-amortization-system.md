# ADR-009: Soporte para Sistema de Amortización Alemán

## Estado
Aceptado

## Extiende
ADR-002: Motor de simulación y sistema francés

## Impacta
- ADR-006: Integration con Semantic Kernel (tool `create_simulation` actualizado)
- ADR-007: Validaciones (nuevos valores permitidos)

## No impacta
- ADR-003: Paginación por cursores
- ADR-004: Rate limiting
- ADR-005: OpenAPI y serialización
- ADR-008: Eliminación y ordenamiento dinámico

## Contexto
Originalmente la plataforma se diseñó y construyó (ADR-002) asumiendo por defecto el cálculo de amortización mediante el **Sistema Francés** (cuota fija). Ha surgido la necesidad de ofrecer otro sistema de amortización que es preferido por muchos clientes debido a los menores costos financieros: el **Sistema Alemán** (cuotas de amortización decrecientes, con capital constante).

Se requiere flexibilidad para calcular distintos cronogramas dentro de la misma arquitectura.

## Decisión
En lugar de crear métodos o servicios sueltos, se adopta el uso del **Factory Pattern** con **Dependency Injection**:
- `IAmortizationCalculator` define la interfaz base de cálculo.
- `FrenchAmortizationCalculator` y `GermanAmortizationCalculator` encapsulan la lógica respectiva.
- `AmortizationCalculatorFactory` provee el calculador adecuado basándose en el tipo ("FIXED" o "GERMAN").

Se ha mantenido la estructura del `ScheduleRow` sin cambios, permitiendo reciclar DTOs y mecanismos de serialización y almacenamiento (`jsonb` de PostgreSQL).

### Reglas del Sistema Alemán Implementadas
- **Capital Constante**: $P_m = P / n$
- **Interés Variable**: $I_m = S_m \times r$ (sobre el saldo pendiente de ese mes)
- **Cuota Mensual Total**: $C_m = P_m + I_m + Seguro$
- **Seguro**: $0.065\%$ mensual sobre saldo (igual que sistema francés).
- *Nota:* En la iteración actual la tasa está cableada en constante.

### Casos Borde Contemplados

| Caso Borde | Comportamiento |
| --- | --- |
| Último Mes | Ajuste del capital amortizado al saldo exactamente pendiente para evitar desvíos (drift) por redondeos acumulados. |
| Tasa de interés = 0% | El cálculo del interés se anula independientemente del saldo, quedando la cuota compuesta solo de su respectiva porción de capital constante ($P / n$) y el seguro mensual calculado sobre el saldo. |

## Consecuencias
- **Positivas**: La plataforma soporta ahora los dos esquemas más populares fácilmente; el diseño Strategy/Factory permite añadir a futuro nuevos sistemas sin modificar lógica existente (Open/Closed Principle).
- **Negativas**: Aumenta ligeramente la complejidad del Handler al requerir dependencia del Factory, requiriendo su actualización respectiva en los unit tests si existiesen.
