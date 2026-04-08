# ADR-001: Arquitectura Inicial del Simulador de Créditos

**Estado:** Aceptado  
**Fecha:** 2026-04-08

## Contexto

Se necesita una Web API para simular créditos personales con cronograma de pagos usando el sistema de amortización francés (cuotas constantes). Debe persistir simulaciones en PostgreSQL y estar preparada para conectarse con un frontend.

## Decisión

### Arquitectura

- **Clean Architecture** con 4 capas: Domain, Application, Infrastructure, WebAPI.
- El flujo de dependencias es siempre hacia adentro: WebAPI → Application ← Infrastructure, Domain no depende de nada.

### Patrones

| Decisión | Justificación |
|---|---|
| **CQRS con MediatR** | Desacopla controllers de lógica de negocio. Facilita testing y extensión (ej: agregar eventos). |
| **FluentValidation + Pipeline Behavior** | Validación centralizada antes de ejecutar handlers, sin ensuciar los controllers. |
| **Repository Pattern** | Abstrae el acceso a datos; permite cambiar el ORM sin afectar Application. |
| **EF Core Code First** | Modelo definido en C#, migraciones versionadas en Git. |
| **Sistema Francés (cuotas fijas)** | Estándar de la industria bancaria latinoamericana para créditos personales. |

### Base de datos

- PostgreSQL 16, desplegada vía Docker Compose.
- Tabla `simulation_history` con columna `schedule_json` tipo `jsonb` para almacenar el cronograma completo.

### API

- `POST /api/simulation` — calcula y persiste.
- `GET /api/simulations/{id}` — recupera simulación previa.
- Swagger habilitado en Development.
- Manejo global de excepciones con `IExceptionHandler` (.NET 8).

## Consecuencias

- (+) Estructura escalable para agregar nuevos tipos de amortización.
- (+) Separación clara de responsabilidades.
- (+) JSON del cronograma almacenado para auditoría sin recalcular.
- (-) Overhead inicial para un MVP; justificado por extensibilidad futura.

## Próximos pasos

1. Agregar unit tests para `AmortizationService` y handlers.
2. Implementar paginación en listado de simulaciones.
3. Agregar autenticación JWT cuando se conecte el frontend.
4. Configurar CI/CD pipeline.
5. Evaluar sistema de amortización alemán como segunda opción.
