# Credits Simulator API

Web API en .NET 8 para simulacion de creditos personales con sistema de amortizacion frances (cuotas constantes). Incluye asistente financiero AI con Gemini via Semantic Kernel.

## Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://docs.docker.com/get-docker/) y Docker Compose
- API Key de Google Gemini (para el asistente AI)

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

### 3. Configurar Gemini API Key

```bash
cd src/CreditsSim.WebAPI
dotnet user-secrets set "Gemini:ApiKey" "TU_API_KEY_AQUI"
```

La API Key se almacena en user-secrets (fuera del repositorio). Si no se configura, la app falla en startup.

### 4. Ejecutar la API

```bash
dotnet run --project src/CreditsSim.WebAPI
```

La migracion se aplica automaticamente al iniciar. La API estara disponible en `http://localhost:5087`.

### 5. Swagger

Navega a [http://localhost:5087/swagger](http://localhost:5087/swagger) para explorar los endpoints.

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

### System prompt

El asistente responde en español, usa las herramientas para datos reales (no inventa cifras), confirma antes de eliminar y redirige preguntas fuera de su dominio.

### Configuracion

```json
// appsettings.json
{
  "Gemini": {
    "ApiKey": "",              // via user-secrets o env var
    "ChatModel": "gemini-2.5-flash"
  }
}
```

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
src/
├── CreditsSim.Domain/            # Entidades, interfaces, query objects
├── CreditsSim.Application/       # CQRS (MediatR), DTOs, validacion, amortizacion
├── CreditsSim.Infrastructure/    # EF Core, PostgreSQL, repositorios, specifications
└── CreditsSim.WebAPI/
    ├── Controllers/              # Simulations + Assistant
    ├── Plugins/SimulationPlugin  # Semantic Kernel tools para Gemini
    ├── Filters/                  # PaginationHeaderFilter
    ├── Middleware/                # GlobalExceptionHandler
    └── Swagger/                  # RequireNonNullableSchemaFilter
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

## Configuracion

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=credits_sim;Username=postgres;Password=postgres"
  },
  "Gemini": {
    "ApiKey": "",
    "ChatModel": "gemini-2.5-flash"
  }
}
```

## Contrato OpenAPI

```
http://localhost:5087/swagger/v1/swagger.json
```

## Detener los servicios

```bash
docker compose down       # mantiene datos
docker compose down -v    # elimina datos
```
