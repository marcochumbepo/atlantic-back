# Guía de Levantamiento en Docker - Atlantic Orders Backend

Guía completa para ejecutar el backend de Atlantic Orders en Docker, tanto en desarrollo como en producción.

## Requisitos

- Docker Desktop instalado y ejecutándose
- Docker Compose 1.29+
- Mínimo 2 GB de RAM disponible
- Puerto 8080 disponible

## Inicio Rápido

```bash
# Desde la carpeta backend/
docker-compose up -d

# Verificar que está running
docker ps

# Ver logs
docker logs atlantic-orders-api

# Acceder a la API
curl http://localhost:8080/health
```

## Estructura de Docker Compose

### docker-compose.yml

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: atlantic-orders-api
    environment:
      # Database
      UseInMemoryDatabase: "true"
      
      # JWT
      Jwt__SecretKey: "tu-clave-secreta-super-larga-de-al-menos-32-caracteres"
      Jwt__Issuer: "AtlanticOrdersApi"
      Jwt__Audience: "AtlanticOrdersApp"
      Jwt__ExpirationMinutes: "60"
      
      # ASP.NET Core
      ASPNETCORE_ENVIRONMENT: "Production"
      ASPNETCORE_URLS: "http://+:8080"
      
      # Logging
      Logging__LogLevel__Default: "Information"
    
    ports:
      - "8080:8080"
    
    restart: unless-stopped
```

## Ciclo de Vida - Comandos Comunes

### Iniciar Contenedor

```bash
# Iniciar en primer plano (verás logs en vivo)
docker-compose up

# Iniciar en background
docker-compose up -d

# Con rebuild de imagen
docker-compose up -d --build

# Mostrar output
docker-compose logs -f
```

### Detener Contenedor

```bash
# Detener sin eliminar
docker-compose stop

# Detener y eliminar contenedor
docker-compose down

# Detener, eliminar y limpiar volúmenes
docker-compose down -v

# Forzar detención
docker-compose kill
```

### Reiniciar

```bash
# Reiniciar el servicio
docker-compose restart

# Reiniciar servicio específico
docker-compose restart api
```

## Verificación de Estado

### Health Check

```bash
# Desde el host
curl http://localhost:8080/health

# Desde dentro del contenedor
docker exec atlantic-orders-api curl http://localhost:8080/health

# Respuesta esperada
{"status":"Healthy","timestamp":"2026-03-05T...","service":"AtlanticOrdersApi"}
```

### Ver Logs

```bash
# Últimas 50 líneas
docker logs atlantic-orders-api --tail 50

# En vivo
docker logs -f atlantic-orders-api

# Logs de una fecha específica
docker logs --since 2024-01-01 atlantic-orders-api

# Últimos 5 minutos
docker logs --since 5m atlantic-orders-api
```

### Inspeccionar Contenedor

```bash
# Ver detalles del contenedor
docker inspect atlantic-orders-api

# Ver estadísticas (CPU, memoria, red)
docker stats atlantic-orders-api

# Ver procesos dentro del contenedor
docker top atlantic-orders-api
```

## Ejecutar Comandos Dentro del Contenedor

```bash
# Acceder a shell del contenedor
docker exec -it atlantic-orders-api /bin/bash

# Ejecutar curl desde el contenedor
docker exec atlantic-orders-api curl http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@123456"}'

# Ver contenido de archivo
docker exec atlantic-orders-api cat /app/appsettings.json
```

## Configuración de Ambiente

### Variables de Entorno

```yaml
# En docker-compose.yml
environment:
  # Database
  UseInMemoryDatabase: "true"  # o "false" para SQL Server
  
  # Configuración de JWT
  Jwt__SecretKey: "tu-clave-secreta-de-al-menos-32-caracteres"
  Jwt__Issuer: "AtlanticOrdersApi"
  Jwt__Audience: "AtlanticOrdersApp"
  Jwt__ExpirationMinutes: "60"
  Jwt__RefreshTokenExpirationDays: "7"
  Jwt__CookieSecureFlag: "false"  # true en producción con HTTPS
  Jwt__CookieSameSite: "Strict"
  Jwt__CookieName: "X-Access-Token"
  Jwt__RefreshCookieName: "X-Refresh-Token"
  
  # ASP.NET Core
  ASPNETCORE_ENVIRONMENT: "Production"  # o "Development"
  ASPNETCORE_URLS: "http://+:8080"
  
  # Logging
  Logging__LogLevel__Default: "Information"
  Logging__LogLevel__Microsoft_EntityFrameworkCore: "Warning"
  Logging__LogLevel__Microsoft_AspNetCore: "Information"
```

### Crear Archivo .env (Alternativo)

```bash
# .env
JWT_SECRET_KEY=tu-clave-secreta-super-larga
ASPNETCORE_ENVIRONMENT=Production
UseInMemoryDatabase=true
```

Luego en docker-compose.yml:

```yaml
environment:
  Jwt__SecretKey: ${JWT_SECRET_KEY}
  ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT}
```

## Database Persistence

### Opción 1: En-Memory (Defecto)

```yaml
environment:
  UseInMemoryDatabase: "true"
```

**Características**:
- ✅ No requiere SQL Server instalado
- ✅ Rápido para desarrollo
- ❌ Datos se pierden al reiniciar contenedor
- ❌ No multiinstancia

### Opción 2: SQL Server en Contenedor

```yaml
services:
  api:
    # ... configuración anterior ...
    environment:
      UseInMemoryDatabase: "false"
      ConnectionStrings__DefaultConnection: "Server=sql-server;Database=AtlanticOrders;User Id=sa;Password=ComplexPassword123!"
    depends_on:
      - sql-server
    
  sql-server:
    image: mcr.microsoft.com/mssql/server:2019-latest
    container_name: atlantic-sql-server
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "ComplexPassword123!"
    ports:
      - "1433:1433"
    volumes:
      - sql-data:/var/opt/mssql

volumes:
  sql-data:
```

**Conexión desde SQL Server Management Studio**:
```
Server: localhost,1433
User: sa
Password: ComplexPassword123!
Database: AtlanticOrders
```

### Opción 3: SQL Server Externo

```yaml
environment:
  UseInMemoryDatabase: "false"
  ConnectionStrings__DefaultConnection: "Server=192.168.1.100;Database=AtlanticOrders;User Id=sa;Password=YourPassword;"
```

## Dockerfile - Entendiendo la Build

```dockerfile
# ==================== BUILD STAGE ====================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copiar archivos de proyecto
COPY ["AtlanticOrders.Api/AtlanticOrders.Api.csproj", "AtlanticOrders.Api/"]
COPY ["AtlanticOrders.Application/AtlanticOrders.Application.csproj", "AtlanticOrders.Application/"]
COPY ["AtlanticOrders.Domain/AtlanticOrders.Domain.csproj", "AtlanticOrders.Domain/"]
COPY ["AtlanticOrders.Infrastructure/AtlanticOrders.Infrastructure.csproj", "AtlanticOrders.Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "AtlanticOrders.Api/AtlanticOrders.Api.csproj"

# Copiar código fuente
COPY . .

# Compilar
RUN dotnet build "AtlanticOrders.Api/AtlanticOrders.Api.csproj" -c Release -o /app/build

# ==================== PUBLISH STAGE ====================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Instalar herramientas (curl para health check)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copiar binarios publicados
COPY --from=build /app/publish .

# Exponer puerto
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Punto de entrada
ENTRYPOINT ["dotnet", "AtlanticOrders.Api.dll"]
```

**Explicación**:
- **Build Stage**: Compila el código (grande, ~1.5 GB)
- **Runtime Stage**: Solo copia binarios compilados (pequeño, ~200 MB)
- **Multi-stage**: Reduce tamaño de imagen final

## Optimización de Imagen

### Tamaño Actual

```bash
# Ver tamaño
docker images backend-api

# Salida esperada
REPOSITORY    TAG       SIZE
backend-api   latest    280MB
```

### Reducir Tamaño

1. **Usar Alpine (más pequeño)**:
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
   ```

2. **Eliminar archivos innecesarios**:
   ```dockerfile
   RUN dotnet publish -c Release -o /app/publish --no-restore --self-contained false
   ```

3. **Usar .dockerignore**:
   ```
   .git
   .vscode
   bin/
   obj/
   .gitignore
   README.md
   ```

## Networking - Comunicación con Otros Servicios

### Acceso desde Host

```bash
# API está en localhost:8080 desde host
curl http://localhost:8080/health
```

### Acceso entre Contenedores

```bash
# Desde otro contenedor, usar nombre de servicio
# Por ejemplo, desde frontend:
fetch('http://api:8080/api/auth/login')
```

## Logs y Debugging

### Configurar Logging en Appsettings

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Debug",
      "Microsoft.AspNetCore": "Information",
      "AtlanticOrders": "Debug"
    }
  }
}
```

### Ver Logs Estructurados

```bash
# JSON logs (si está configurado)
docker logs atlantic-orders-api | jq .

# Filtrar por nivel
docker logs atlantic-orders-api | grep "ERROR"

# Guardar logs en archivo
docker logs atlantic-orders-api > app.log
```

## Performance y Recursos

### Limitar Recursos del Contenedor

```yaml
services:
  api:
    # ... configuración ...
    resources:
      limits:
        cpus: '1'           # 1 CPU
        memory: 512M        # 512 MB
      reservations:
        cpus: '0.5'         # Minimum
        memory: 256M
```

### Monitorear Recursos

```bash
# Ver uso de CPU y memoria
docker stats atlantic-orders-api --no-stream

# Formato
# CONTAINER    CPU %    MEM USAGE / LIMIT    MEM %    NET I/O    BLOCK I/O
# atlantic...  0.10%    145.3MiB / 512MiB    28.39%   6.5kB      0B
```

## Seguridad en Docker

### No Ejecutar como Root

```dockerfile
RUN useradd -m -u 1000 appuser
USER appuser
```

### Secret Management

```bash
# Docker Secrets (producción)
echo "tu-secret-key" | docker secret create jwt_secret -

# En compose
secrets:
  jwt_secret:
    external: true
```

### Network Aislada

```yaml
networks:
  atlantic-network:
    driver: bridge

services:
  api:
    networks:
      - atlantic-network
```

## Troubleshooting

### Contenedor exits inmediatamente

```bash
# Ver logs
docker logs atlantic-orders-api

# Posibles causas:
# - JWT Secret key no configurada
# - Puerto ya en uso
# - Error en appsettings.json
```

**Solución**:

```bash
# Verificar configuración
docker inspect atlantic-orders-api | grep -A 20 "Env"

# Reintentar con variables correctas
docker-compose down
docker-compose up -d --build
```

### No puedo conectarme a la API

```bash
# Verificar que el puerto está mapeado
docker port atlantic-orders-api

# Debería mostrar:
# 8080/tcp -> 0.0.0.0:8080

# Si no muestra nada, revisar docker-compose.yml
```

### Error de base de datos

```bash
# Si usa SQL Server en contenedor
docker-compose down
docker volume rm <volume-name>  # Eliminar datos persistentes
docker-compose up -d --build
```

### Falta de memoria

```bash
# Ver uso actual
docker stats

# Aumentar límites en docker-compose.yml
resources:
  limits:
    memory: 1G
```

## Deployment a Producción

### Variables de Producción

```yaml
environment:
  # Seguridad
  ASPNETCORE_ENVIRONMENT: "Production"
  Jwt__CookieSecureFlag: "true"      # Solo HTTPS
  Jwt__CookieSameSite: "Strict"      # CSRF protection
  
  # Database
  UseInMemoryDatabase: "false"       # SQL Server
  ConnectionStrings__DefaultConnection: "Server=prod-sql;Database=Atlantic;..."
  
  # JWT con clave fuerte
  Jwt__SecretKey: "CHANGE-THIS-TO-STRONG-SECRET-KEY-MIN-32-CHARS"
  
  # Logging
  Logging__LogLevel__Default: "Warning"  # Menos verboso
```

### Health Check Crítico

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

### Reverse Proxy (Nginx)

```nginx
upstream atlantic_backend {
  server atlantic-orders-api:8080;
}

server {
  listen 443 ssl http2;
  server_name api.atlantic.com;
  
  ssl_certificate /etc/ssl/cert.pem;
  ssl_certificate_key /etc/ssl/key.pem;
  
  location / {
    proxy_pass http://atlantic_backend;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
  }
}
```

## Checklista de Deployment

- [ ] JWT Secret Key configurada y fuerte
- [ ] ASPNETCORE_ENVIRONMENT = "Production"
- [ ] UseInMemoryDatabase = "false"
- [ ] SQL Server configurado con backups
- [ ] Logging configurado para producción
- [ ] Health check habilitado
- [ ] Recursos limitados (CPU, Memory)
- [ ] Network aislada
- [ ] HTTPS/SSL configurado
- [ ] Backup y disaster recovery plan

## Referencias

- [Docker Documentation](https://docs.docker.com/)
- [ASP.NET Core Docker](https://github.com/dotnet/dotnet-docker)
- [Docker Compose Specification](https://github.com/compose-spec/compose-spec)
- [SQL Server on Linux](https://docs.microsoft.com/en-us/sql/linux/)

---

**Última actualización**: Marzo 5, 2026
**Versión**: 1.0
