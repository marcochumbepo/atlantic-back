# Guía de Instalación - Atlantic Orders Backend

Guía completa para instalar y configurar el proyecto Atlantic Orders Backend en tu entorno local.

## Requisitos Previos

### Sistema Operativo
- Windows 10/11, macOS (10.14+) o Linux (Ubuntu 20.04+)
- Acceso a terminal/consola
- Git instalado

### Software Requerido

| Software | Versión | Propósito |
|----------|---------|----------|
| .NET SDK | 8.0+ | Runtime y compilador |
| Docker & Docker Compose | Latest | Containerización |
| SQL Server (opcional) | 2019+ | Base de datos |
| Git | 2.30+ | Control de versiones |
| Visual Studio Code / IDE | Latest | Desarrollo |
| Postman | Latest | Testing de API |

### Verificar Instalaciones

```bash
# Verificar .NET
dotnet --version

# Verificar Docker
docker --version
docker-compose --version

# Verificar Git
git --version
```

## Paso 1: Clonar el Repositorio

```bash
# Clonar el repositorio
git clone <your-repository-url>
cd Atlantic

# Navegar a la carpeta del backend
cd backend
```

## Paso 2: Restaurar Dependencias

```bash
# Desde la carpeta backend/
dotnet restore
```

**Qué hace**: Descarga todos los paquetes NuGet especificados en los archivos `.csproj`

**Paquetes principales que se instalarán**:
- `Microsoft.EntityFrameworkCore` - ORM para base de datos
- `Microsoft.AspNetCore.Authentication.JwtBearer` - Autenticación JWT
- `BCrypt.Net-Core` - Hash de contraseñas
- `FluentValidation` - Validación de datos
- `AutoMapper` - Mapeo de objetos

## Paso 3: Configurar Variables de Entorno

### Opción A: Usar User Secrets (Recomendado para desarrollo)

```bash
# Desde la carpeta backend/AtlanticOrders.Api/
cd AtlanticOrders.Api

# Inicializar user secrets
dotnet user-secrets init

# Establecer el secreto JWT (IMPORTANTE)
dotnet user-secrets set "Jwt:SecretKey" "tu-clave-secreta-super-larga-de-al-menos-32-caracteres-para-hs256-produccion"

# Verificar que se guardó
dotnet user-secrets list
```

**¿Por qué User Secrets?**
- No se versiona en Git (seguro)
- Solo accesible en tu máquina
- Ideal para desarrollo local
- Fácil de cambiar

### Opción B: Variables de Entorno del Sistema

```bash
# Windows (PowerShell como Admin)
[Environment]::SetEnvironmentVariable("JWT_SECRET_KEY", "tu-clave-secreta-aqui", "User")

# macOS/Linux
export JWT_SECRET_KEY="tu-clave-secreta-aqui"
```

### Opción C: Archivo appsettings.json (NO para producción)

```json
{
  "Jwt": {
    "SecretKey": "tu-clave-aqui-pero-NO-en-produccion",
    "Issuer": "AtlanticOrdersApi",
    "Audience": "AtlanticOrdersApp",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

⚠️ **NUNCA commits this to version control for production keys**

## Paso 4: Estructura de Directorios

```
backend/
├── AtlanticOrders.Api/              # Punto de entrada (API)
│   ├── Controllers/                 # Endpoints HTTP
│   ├── Middleware/                  # Procesamiento de requests
│   ├── Properties/                  # Configuración de launch
│   ├── Program.cs                   # Configuración de startup
│   ├── appsettings.json            # Configuración por ambiente
│   └── Dockerfile                   # Build para Docker
│
├── AtlanticOrders.Application/      # Lógica de negocio
│   ├── DTOs/                        # Data Transfer Objects
│   ├── Services/                    # Servicios de aplicación
│   ├── Mappings/                    # AutoMapper profiles
│   └── Validators/                  # Validaciones con FluentValidation
│
├── AtlanticOrders.Domain/           # Entidades y repositorios
│   ├── Entities/                    # Modelos de dominio
│   └── Repositories/                # Interfaces de repositorio
│
├── AtlanticOrders.Infrastructure/   # Implementaciones
│   ├── Persistence/                 # Entity Framework Core
│   ├── Repositories/                # Implementación de repositorios
│   ├── Security/                    # JWT, BCrypt, Rate Limiting
│   └── Configuration/               # Configuración EF Core
│
├── docker-compose.yml               # Orquestación de contenedores
└── documentacion/                   # Este directorio (documentación)
```

## Paso 5: Ejecutar en Desarrollo Local

### Opción A: Usar .NET CLI

```bash
# Desde la carpeta backend/
cd AtlanticOrders.Api

# Ejecutar la aplicación
dotnet run

# La API estará disponible en:
# https://localhost:5001
# http://localhost:5000
# Swagger UI: https://localhost:5001/swagger
```

### Opción B: Usar Visual Studio Code

```bash
# 1. Instalar extensión C# (ms-dotnettools.csharp)
# 2. Abrir carpeta: File > Open Folder > backend
# 3. Press F5 para ejecutar
# 4. Seleccionar ".NET 8.0" como runtime
```

### Opción C: Usar Visual Studio 2022

```bash
# 1. Open > Project/Solution
# 2. Navega a backend/AtlanticOrders.Api/AtlanticOrders.Api.csproj
# 3. Press F5 o Debug > Start Debugging
```

## Paso 6: Ejecutar Tests (Opcional)

```bash
# Si hay proyecto de tests
dotnet test

# Con cobertura
dotnet test /p:CollectCoverage=true
```

## Paso 7: Verificar Instalación

```bash
# Hacer request a health check
curl https://localhost:5001/health

# Respuesta esperada:
# {"status":"Healthy","timestamp":"2026-03-05T...","service":"AtlanticOrdersApi"}
```

## Configuración de Base de Datos

### Opción 1: En-Memory (Defecto)

```json
{
  "UseInMemoryDatabase": true
}
```

**Ventajas**: No requiere instalación
**Desventajas**: Datos se pierden al reiniciar

### Opción 2: SQL Server LocalDB (Windows)

```bash
# Instalar LocalDB si no está instalado
# https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb

# Crear connection string en appsettings.json
{
  "UseInMemoryDatabase": false,
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AtlanticOrdersDb;Trusted_Connection=true;"
  }
}

# Ejecutar migraciones
dotnet ef database update --project AtlanticOrders.Infrastructure

# O si necesitas crear desde cero
dotnet ef migrations add InitialCreate --project AtlanticOrders.Infrastructure
dotnet ef database update --project AtlanticOrders.Infrastructure
```

### Opción 3: SQL Server Docker

```bash
# Ejecutar SQL Server en Docker
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=ComplexPassword123!" \
  -p 1433:1433 \
  -d mcr.microsoft.com/mssql/server:2019-latest

# Connection string
"Server=localhost;Database=AtlanticOrdersDb;User Id=sa;Password=ComplexPassword123;"
```

## Solución de Problemas

### Error: "The type or namespace name could not be found"

**Solución**:
```bash
# Limpiar y restaurar
dotnet clean
dotnet restore
```

### Error: "JWT:SecretKey no está configurada"

**Solución**:
```bash
# Verificar User Secrets
dotnet user-secrets list

# Si está vacío, establecer
dotnet user-secrets set "Jwt:SecretKey" "tu-clave-secreta-de-32-caracteres"
```

### Error: "Port 5001 already in use"

**Windows**:
```powershell
# Encontrar proceso
Get-Process | Where-Object {$_.ProcessName -like "*dotnet*"}

# Matar proceso
Stop-Process -Id <PID> -Force
```

**macOS/Linux**:
```bash
# Encontrar proceso
lsof -i :5001

# Matar proceso
kill -9 <PID>
```

### Error: "Database error during startup"

```bash
# Limpiar y recrear
dotnet ef database drop --project AtlanticOrders.Infrastructure --force
dotnet ef database update --project AtlanticOrders.Infrastructure
```

## Configuración de IDE

### Visual Studio Code

**1. Crear .vscode/launch.json**:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/AtlanticOrders.Api/bin/Debug/net8.0/AtlanticOrders.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/AtlanticOrders.Api",
      "preLaunchTask": "build",
      "env": {},
      "stopAtEntry": false,
      "console": "internalConsole"
    }
  ]
}
```

**2. Crear .vscode/tasks.json**:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": ["build"],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

### Visual Studio 2022

- Extensiones recomendadas:
  - `Microsoft.VisualStudio.ProjectSystem.PrivateProjectSystemBase`
  - `Microsoft.Net.Compilers.Roslyn`

**Configurar Debug**:
- Project Properties > Debug > Launch profiles > https
- Set port to 5001

## Próximos Pasos

1. **Levantar en Docker**: Ver `02-DOCKER_DEPLOYMENT.md`
2. **Testing**: Ver `03-POSTMAN_COLLECTION.md`
3. **Arquitectura**: Ver `04-ARCHITECTURE.md`
4. **Decisiones Técnicas**: Ver `05-TECHNICAL_DECISIONS.md`

## Referencias

- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/)
- [ASP.NET Core Security](https://learn.microsoft.com/en-us/aspnet/core/security/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [JWT Documentation](https://jwt.io/)

## Soporte

Si tienes problemas durante la instalación:

1. Verifica que cumples todos los requisitos previos
2. Revisa el log de error completo
3. Intenta ejecutar `dotnet clean` y `dotnet restore`
4. Consulta la sección de "Solución de Problemas"

---

**Última actualización**: Marzo 5, 2026
**Versión**: 1.0
