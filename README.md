# Credits Simulator API

Web API en .NET 8 para simulacion de creditos personales con sistema de amortizacion frances (cuotas constantes).

## Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://docs.docker.com/get-docker/) y Docker Compose

## Primeros pasos

### 1. Clonar el repositorio

```bash
git clone https://github.com/tu-usuario/credits-sim-back.git
cd credits-sim-back
```

### 2. Levantar la base de datos

```bash
docker compose up -d
```

Inicia un contenedor PostgreSQL 16 en `localhost:5432` con credenciales por defecto (`postgres/postgres`).

### 3. Ejecutar la API

```bash
dotnet run --project src/CreditsSim.WebAPI
```

La migracion se aplica automaticamente al iniciar. La API estara disponible en `http://localhost:5087`.

### 4. Swagger

Navega a [http://localhost:5087/swagger](http://localhost:5087/swagger) para explorar los endpoints.

## Endpoints

### Crear simulacion

```
POST /api/simulation
```

**Body:**

```json
{
  "amount": 50000,
  "termMonths": 12,
  "annualRate": 18.5,
  "installmentType": "FIXED"
}
```

**Respuesta (201 Created):**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "amount": 50000,
  "termMonths": 12,
  "annualRate": 18.5,
  "installmentType": "FIXED",
  "schedule": [
    {
      "month": 1,
      "payment": 4613.78,
      "principal": 3842.95,
      "interest": 770.83,
      "insurance": 32.50,
      "balance": 46157.05
    }
  ],
  "createdAt": "2026-04-08T12:00:00Z"
}
```

### Consultar simulacion

```
GET /api/simulations/{id}
```

### Listar simulaciones (cursor-based pagination)

```
GET /api/simulations?pageSize=10&sortOrder=desc
```

**Parametros de cursor (para pagina siguiente):**

| Parametro | Tipo | Descripcion |
|---|---|---|
| `pageSize` | int | Elementos por pagina (1-100, default 10) |
| `sortOrder` | string | `desc` (default) o `asc` |
| `cursorCreatedAt` | DateTime | Timestamp del ultimo item de la pagina anterior |
| `cursorId` | Guid | ID del ultimo item de la pagina anterior (desempate) |

**Filtros opcionales (AND):**

| Parametro | Tipo | Descripcion |
|---|---|---|
| `amountMin` | decimal | Monto minimo (inclusive) |
| `amountMax` | decimal | Monto maximo (inclusive) |
| `termMonths` | int | Plazo exacto en meses |
| `annualRateMin` | decimal | Tasa minima (%) |
| `annualRateMax` | decimal | Tasa maxima (%) |
| `installmentType` | string | Tipo de cuota (FIXED) |
| `createdFrom` | DateTime | Creado desde (UTC) |
| `createdTo` | DateTime | Creado hasta (UTC) |

**Respuesta (200 OK):**

```
Headers:
  X-Total-Count: 42          (solo en primera pagina)
  X-Page-Size: 10
  X-Has-Next-Page: true
  X-Next-Cursor-CreatedAt: 2026-04-09T21:06:57.5667060Z
  X-Next-Cursor-Id: 96acfe26-3347-4ea0-a1d0-0606ce1fb48a
  Cache-Control: private,max-age=30

Body:
  [{ ... }, { ... }]          (array de SimulationSummary)
```

**Flujo de paginacion:**

```
1. Primera pagina:
   GET /api/simulations?pageSize=10

2. Segunda pagina (usando cursores del header):
   GET /api/simulations?pageSize=10&cursorCreatedAt=2026-04-09T21:06:57Z&cursorId=96acfe26-...

3. Siguiente pagina: repetir con nuevos cursores del header
4. Ultima pagina: X-Has-Next-Page: false
```

## Arquitectura

```
src/
├── CreditsSim.Domain/            # Entidades, interfaces, query objects
├── CreditsSim.Application/       # CQRS (MediatR), DTOs, validacion, amortizacion
├── CreditsSim.Infrastructure/    # EF Core, PostgreSQL, repositorios, specifications
└── CreditsSim.WebAPI/            # Controllers, filters, middleware, Swagger
```

### Patrones

- **Clean Architecture** con dependencias hacia adentro
- **CQRS** via MediatR (commands/queries separados)
- **Cursor-based pagination** con indice compuesto `(created_at DESC, id DESC)`
- **Pagination headers** via `PaginationHeaderFilter` (ActionFilter automatico)
- **Specification pattern** para filtros de busqueda
- **FluentValidation** con pipeline behavior
- **Rate limiting** nativo de .NET 8 (FixedWindow, 10 req/min por IP)
- **Response caching** client-side (30s, `private`)

## Configuracion

La cadena de conexion se encuentra en `src/CreditsSim.WebAPI/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=credits_sim;Username=postgres;Password=postgres"
  }
}
```

## Health check

```
GET /health
```

Exento de rate limiting. Retorna `{ "status": "healthy" }`.

## Contrato OpenAPI

```
http://localhost:5087/swagger/v1/swagger.json
```

## Detener los servicios

```bash
docker compose down       # mantiene datos
docker compose down -v    # elimina datos
```
