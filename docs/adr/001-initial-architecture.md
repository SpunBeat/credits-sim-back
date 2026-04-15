# ADR-001: Arquitectura Inicial del Simulador de Creditos

**Estado:** Aceptado  
**Fecha:** 2026-04-08  
**Actualizado:** 2026-04-15

## Contexto

Se necesita una Web API para simular creditos personales con cronograma de pagos usando el sistema de amortizacion frances (cuotas constantes). Debe persistir simulaciones en PostgreSQL y estar preparada para conectarse con un frontend Angular.

## Decision

### Arquitectura

- **Clean Architecture** con 4 capas: Domain, Application, Infrastructure, WebAPI.
- El flujo de dependencias es siempre hacia adentro: WebAPI → Application ← Infrastructure. Domain no depende de nada.

### Patrones

| Decision | Justificacion |
|---|---|
| **CQRS con MediatR** | Desacopla controllers de logica de negocio. Facilita testing y extension. |
| **FluentValidation + Pipeline Behavior** | Validacion centralizada antes de ejecutar handlers. |
| **Repository Pattern** | Abstrae el acceso a datos; permite cambiar el ORM sin afectar Application. |
| **EF Core Code First** | Modelo definido en C#, migraciones versionadas en Git. |
| **Sistema Frances (cuotas fijas)** | Estandar de la industria bancaria latinoamericana para creditos personales. |

### Base de datos

- PostgreSQL 16, desplegada via Docker Compose.
- Tabla `simulation_history` con columna `schedule_json` tipo `jsonb` para almacenar el cronograma completo.
- Auto-migracion en startup (`db.Database.Migrate()`).

### API

- `POST /api/simulation` — calcula y persiste.
- `GET /api/simulations/{id}` — recupera simulacion previa.
- `DELETE /api/simulations/{id}` — eliminacion fisica.
- `GET /api/simulations` — listado con cursor pagination, sort y filtros.
- `POST /api/assistant/chat` — asistente AI con Gemini.
- Swagger habilitado en Development con XML docs.
- Manejo global de excepciones con `IExceptionHandler` (.NET 8).

## Consecuencias

- (+) Estructura escalable para agregar nuevos tipos de amortizacion.
- (+) Separacion clara de responsabilidades.
- (+) JSON del cronograma almacenado para auditoria sin recalcular.
- (-) Overhead inicial para un MVP; justificado por extensibilidad futura.
