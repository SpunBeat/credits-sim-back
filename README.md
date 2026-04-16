# Credits Simulator API

Web API en .NET 8 para simulacion de creditos personales con sistema de amortizacion frances (cuotas constantes). Incluye asistente financiero AI con Gemini via Semantic Kernel.

## Requisitos previos

- [Docker](https://docs.docker.com/get-docker/) y Docker Compose
- API Key de Google Gemini (para el asistente AI)

> No se requiere .NET SDK instalado — el proyecto se compila y ejecuta dentro de Docker.

## Inicio rapido

```bash
# 1. Clonar
git clone https://github.com/tu-usuario/credits-sim-back.git
cd credits-sim-back

# 2. Configurar variables de entorno
cp .env.example .env
# Editar .env y agregar tu GEMINI_API_KEY

# 3. Levantar todo
docker compose up -d

# 4. Verificar
curl http://localhost:5087/health
```

La API estara disponible en `http://localhost:5087`. PostgreSQL en `localhost:5432`.

## Configuracion (.env)

El archivo `.env` contiene toda la configuracion necesaria. Copiar `.env.example` como punto de partida:

```env
# Base de datos
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=credits_sim

# API
ASPNETCORE_ENVIRONMENT=Development
CONNECTION_STRING=Host=postgres;Port=5432;Database=credits_sim;Username=postgres;Password=postgres

# Gemini AI
GEMINI_API_KEY=tu_api_key_aqui    # <-- obligatorio
GEMINI_CHAT_MODEL=gemini-2.5-flash
```

> **Importante:** `CONNECTION_STRING` usa `Host=postgres` (nombre del servicio Docker), no `localhost`. Esto se resuelve automaticamente dentro de la red de Docker.

## Docker

### Arquitectura de contenedores

```
┌─────────────────────────────┐
│  credits-sim-api (:5087)    │──── .NET 8 Alpine (143MB)
│  Auto-migración al iniciar  │
└────────────┬────────────────┘
             │ depends_on (healthy)
┌────────────▼────────────────┐
│  credits-sim-db (:5432)     │──── PostgreSQL 16 Alpine
│  Healthcheck: pg_isready    │
└─────────────────────────────┘
```

- **Imagen base:** `mcr.microsoft.com/dotnet/aspnet:8.0-alpine` — multi-arch (amd64 + arm64)
- **Tamaño final:** ~143MB (Alpine + ICU para globalization)
- **Apple Silicon:** compatible nativo con M1/M2/M3/M4 (arm64)
- **Healthcheck:** PostgreSQL reporta healthy antes de que la API arranque
- **Migraciones:** se aplican automaticamente en startup

### Comandos

```bash
docker compose up -d            # levantar en background
docker compose up -d --build    # reconstruir imagen + levantar
docker compose logs -f api      # ver logs de la API en tiempo real
docker compose logs -f postgres # ver logs de PostgreSQL
docker compose ps               # ver estado de contenedores
docker compose down             # detener (mantiene datos)
docker compose down -v          # detener + borrar volumenes (datos)
```

### Reconstruir tras cambios en codigo

```bash
docker compose up -d --build
```

## Desarrollo local (sin Docker para la API)

Si prefieres ejecutar la API directamente con el SDK de .NET (requiere .NET 8 SDK instalado):

```bash
# Levantar solo PostgreSQL con Docker
docker compose up -d postgres

# Configurar Gemini API Key via user-secrets
cd src/CreditsSim.WebAPI
dotnet user-secrets set "Gemini:ApiKey" "TU_API_KEY_AQUI"

# Ejecutar la API
dotnet run --project src/CreditsSim.WebAPI
```

> En este modo, la connection string de `appsettings.json` usa `Host=localhost` (no `Host=postgres`).

## Swagger

Navega a [http://localhost:5087/swagger](http://localhost:5087/swagger) para explorar y probar los endpoints.

## Endpoints

### Simulaciones

| Endpoint | Metodo | Descripcion |
|---|---|---|
| `/api/simulation` | POST | Crea una simulacion y devuelve el cronograma |
| `/api/simulations/{id}` | GET | Recupera una simulacion por GUID |
| `/api/simulations/{id}` | DELETE | Elimina una simulacion (204/404) |
| `/api/simulations` | GET | Lista con cursor pagination, sort y filtros |

### Asistente AI

| Endpoint | Metodo | Descripcion |
|---|---|---|
| `/api/assistant/chat` | POST | Chat con el asistente financiero (Gemini) |

### Health check

```
GET /health
```

Exento de rate limiting. Retorna `{ "status": "healthy" }`.

## Asistente AI (Semantic Kernel + Gemini)

El endpoint `POST /api/assistant/chat` conecta con Gemini via Semantic Kernel con function calling habilitado.

### Request

```json
{
  "message": "Simulame un credito de 60000 pesos a 24 meses al 18% fijo",
  "conversationHistory": []
}
```

### Response

```json
{
  "reply": "Tu credito de $60,000 a 24 meses... cuota mensual: $3,034.45...",
  "toolsUsed": ["create_simulation"]
}
```

### Tools disponibles (SimulationPlugin)

| Tool | Descripcion |
|---|---|
| `create_simulation` | Crea una simulacion con monto, plazo y tasa |
| `get_simulation` | Obtiene detalle completo con cronograma por ID |
| `list_simulations` | Lista las N simulaciones mas recientes |
| `compare_simulations` | Compara KPIs de multiples simulaciones lado a lado |
| `delete_simulation` | Elimina una simulacion por ID (requiere confirmacion del usuario) |

## Crear simulacion

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
  "schedule": [{ "month": 1, "payment": 4613.78, "principal": 3842.95, "interest": 770.83, "insurance": 32.50, "balance": 46157.05 }],
  "createdAt": "2026-04-08T12:00:00Z"
}
```

## Listar simulaciones (cursor-based pagination)

```
GET /api/simulations?pageSize=10&sortBy=createdAt&sortOrder=desc
```

**Parametros:**

| Parametro | Tipo | Descripcion |
|---|---|---|
| `pageSize` | int | Elementos por pagina (1-100, default 10) |
| `sortBy` | string | Columna: `createdAt`, `amount`, `termMonths`, `annualRate`, `installmentType` |
| `sortOrder` | string | `desc` (default) o `asc` |
| `cursorCreatedAt` | DateTime | Cursor: timestamp del ultimo item |
| `cursorId` | Guid | Cursor: ID del ultimo item (desempate) |

**Filtros opcionales (AND):** `amountMin`, `amountMax`, `termMonths`, `annualRateMin`, `annualRateMax`, `installmentType`, `createdFrom`, `createdTo`

**Response headers:**

```
X-Total-Count: 42          (solo en primera pagina)
X-Has-Next-Page: true
X-Next-Cursor-CreatedAt: 2026-04-09T21:06:57Z
X-Next-Cursor-Id: 96acfe26-...
Cache-Control: private,max-age=30
```

## Arquitectura

```
credits-sim-back/
├── Dockerfile                    # Multi-stage: SDK Alpine → ASP.NET Alpine
├── compose.yml                   # PostgreSQL + API
├── .env.example                  # Template de variables de entorno
└── src/
    ├── CreditsSim.Domain/        # Entidades, interfaces, query objects
    ├── CreditsSim.Application/   # CQRS (MediatR), DTOs, validacion, amortizacion
    ├── CreditsSim.Infrastructure/ # EF Core, PostgreSQL, repositorios, specifications
    └── CreditsSim.WebAPI/
        ├── Controllers/          # Simulations + Assistant
        ├── Plugins/              # SimulationPlugin (Semantic Kernel tools)
        ├── Filters/              # PaginationHeaderFilter
        ├── Middleware/            # GlobalExceptionHandler
        └── Swagger/              # RequireNonNullableSchemaFilter
```

### Patrones

- **Clean Architecture** con dependencias hacia adentro
- **CQRS** via MediatR (commands/queries separados)
- **Semantic Kernel** con Gemini (function calling + SimulationPlugin)
- **Cursor-based pagination** con indice compuesto `(created_at DESC, id DESC)`
- **Pagination headers** via ActionFilter automatico
- **Specification pattern** para filtros de busqueda
- **FluentValidation** con pipeline behavior
- **Rate limiting** nativo (Simulations: 10 req/min, Assistant: 20 req/min, por IP)
- **Response caching** client-side (30s, `private`)

## Contrato OpenAPI

```
http://localhost:5087/swagger/v1/swagger.json
```
