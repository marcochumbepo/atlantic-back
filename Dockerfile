# ==================== BUILD STAGE ====================
# Utiliza SDK de .NET 8 para compilar la aplicación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copiar archivos de proyecto para restaurar dependencias
# Este paso se cachea si no cambian los archivos .csproj
COPY ["AtlanticOrders.Api/AtlanticOrders.Api.csproj", "AtlanticOrders.Api/"]
COPY ["AtlanticOrders.Application/AtlanticOrders.Application.csproj", "AtlanticOrders.Application/"]
COPY ["AtlanticOrders.Domain/AtlanticOrders.Domain.csproj", "AtlanticOrders.Domain/"]
COPY ["AtlanticOrders.Infrastructure/AtlanticOrders.Infrastructure.csproj", "AtlanticOrders.Infrastructure/"]

# Restaurar dependencias NuGet
RUN dotnet restore "AtlanticOrders.Api/AtlanticOrders.Api.csproj"

# Copiar código fuente completo
COPY . .

# Compilar en modo Release para optimizar rendimiento
RUN dotnet build "AtlanticOrders.Api/AtlanticOrders.Api.csproj" -c Release -o /app/build

# ==================== PUBLISH STAGE ====================
# Publicar la aplicación (genera binarios listos para ejecución)
FROM build AS publish

RUN dotnet publish "AtlanticOrders.Api/AtlanticOrders.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# ==================== RUNTIME STAGE ====================
# Imagen final: usa solo ASP.NET runtime (mucho más pequeña)
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Instalar curl para healthcheck (opcional pero recomendado)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copiar binarios publicados desde el stage anterior
COPY --from=publish /app/publish .

# Variables de entorno por defecto (pueden ser sobrescritas)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV UseInMemoryDatabase=true

# Puerto 8080 (como se requiere)
EXPOSE 8080

# Healthcheck: verifica cada 30 segundos si la API está disponible
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Comando de inicio
ENTRYPOINT ["dotnet", "AtlanticOrders.Api.dll"]
