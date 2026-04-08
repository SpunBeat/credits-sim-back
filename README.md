# Credits Simulator API

Web API en .NET 8 para simulación de créditos personales con sistema de amortización francés (cuotas constantes).

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

Esto inicia un contenedor PostgreSQL 16 en `localhost:5432` con las credenciales por defecto (`postgres/postgres`).

### 3. Ejecutar la API

```bash
dotnet run --project src/CreditsSim.WebAPI
```

La migración de base de datos se aplica automáticamente al iniciar la aplicación.

La API estará disponible en `http://localhost:5087`.

### 4. Abrir Swagger

Navega a [http://localhost:5087/swagger](http://localhost:5087/swagger) para explorar y probar los endpoints.

## Endpoints

### Crear simulación

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

### Consultar simulación

```
GET /api/simulations/{id}
```

## Contrato OpenAPI

El archivo de especificación se genera automáticamente y está disponible en:

```
http://localhost:5087/swagger/v1/swagger.json
```

## Estructura del proyecto

```
src/
├── CreditsSim.Domain/          # Entidades e interfaces
├── CreditsSim.Application/     # CQRS, DTOs, validación, lógica financiera
├── CreditsSim.Infrastructure/  # EF Core, PostgreSQL, repositorios
└── CreditsSim.WebAPI/          # Controllers, middleware, configuración
```

## Configuración

La cadena de conexión se encuentra en `src/CreditsSim.WebAPI/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=credits_sim;Username=postgres;Password=postgres"
  }
}
```

## Detener los servicios

```bash
docker compose down
```

Para eliminar también los datos persistidos:

```bash
docker compose down -v
```
