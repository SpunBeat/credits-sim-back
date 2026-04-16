# ── Stage 1: Build ────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj files first for layer caching. Solo los proyectos de src/ son
# necesarios para el runtime — tests/ se excluye del build de produccion.
COPY src/CreditsSim.Domain/CreditsSim.Domain.csproj src/CreditsSim.Domain/
COPY src/CreditsSim.Application/CreditsSim.Application.csproj src/CreditsSim.Application/
COPY src/CreditsSim.Infrastructure/CreditsSim.Infrastructure.csproj src/CreditsSim.Infrastructure/
COPY src/CreditsSim.WebAPI/CreditsSim.WebAPI.csproj src/CreditsSim.WebAPI/

# Restore transitivo desde WebAPI (arrastra Domain, Application, Infrastructure).
# Evita depender del .sln, que incluye tests/ no presente en el contexto de build.
RUN dotnet restore src/CreditsSim.WebAPI/CreditsSim.WebAPI.csproj

# Copy source trees (src only — tests/ se ignora via .dockerignore)
COPY src/ src/
RUN dotnet publish src/CreditsSim.WebAPI/CreditsSim.WebAPI.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Alpine needs ICU for globalization (currency formatting, etc.)
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CreditsSim.WebAPI.dll"]
