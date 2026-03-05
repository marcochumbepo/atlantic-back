# Arquitectura y Tecnologías - Atlantic Orders Backend

Documentación completa sobre la arquitectura, patrones de diseño y tecnologías utilizadas en Atlantic Orders Backend.

## Tabla de Contenidos

1. [Visión General](#visión-general)
2. [Arquitectura de Capas](#arquitectura-de-capas)
3. [Patrones de Diseño](#patrones-de-diseño)
4. [Tecnologías Utilizadas](#tecnologías-utilizadas)
5. [Flujos de Datos](#flujos-de-datos)
6. [Diagrama de Componentes](#diagrama-de-componentes)

## Visión General

Atlantic Orders Backend es una API RESTful construida sobre una arquitectura limpia (Clean Architecture) con .NET 8 que implementa los siguientes principios:

- **Separación de Responsabilidades**: Cada capa tiene una responsabilidad clara
- **Independencia de Frameworks**: La lógica de negocio no depende de frameworks específicos
- **Testabilidad**: Fácil de hacer testing unitarios e integrados
- **Mantenibilidad**: Código organizado y escalable

### Stack Tecnológico

```
┌─────────────────────────────────────────────────────────────┐
│                    ASP.NET Core 8 (API REST)                │
├─────────────────────────────────────────────────────────────┤
│                   Clean Architecture Layers                 │
├────────────────┬──────────────────┬──────────────┬──────────┤
│ Presentation   │ Application      │ Domain       │ Infra    │
│ (Controllers)  │ (Services, DTOs) │ (Entities)   │ (DB)     │
├────────────────┴──────────────────┴──────────────┴──────────┤
│                  Cross-Cutting Concerns                     │
│         (Authentication, Logging, Validation, etc)          │
├─────────────────────────────────────────────────────────────┤
│            Database (In-Memory / SQL Server)                │
└─────────────────────────────────────────────────────────────┘
```

## Arquitectura de Capas

### 1. **Domain Layer** (Capa de Dominio)

**Ubicación**: `AtlanticOrders.Domain/`

**Responsabilidad**: Contiene las entidades principales y las interfaces de repositorio.

**Componentes**:

#### Entidades (Entities/)

```
User.cs
├── Id: int
├── Email: string (unique)
├── PasswordHash: string (BCrypt)
├── FullName: string
├── Role: string ("Admin" | "User")
├── IsActive: bool
├── CreatedAt: DateTime
├── UpdatedAt: DateTime
└── LastLoginAt: DateTime

RefreshToken.cs
├── Id: int
├── UserEmail: string
├── Token: string
├── ExpiresAt: DateTime
├── CreatedAt: DateTime
└── IsRevoked: bool

Pedido.cs
├── Id: int
├── NumeroPedido: string
├── Cliente: string
├── Fecha: DateTime
├── Total: decimal
├── Estado: string
└── EliminadoLogicamente: bool
```

#### Repositorio (Repositories/)

```csharp
// Interfaces que definen los contratos
public interface IUserRepository
{
    Task<User> GetByEmailAsync(string email);
    Task<User> GetByIdAsync(int id);
    Task CreateAsync(User user);
    Task UpdateAsync(User user);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken> GetByTokenAsync(string token);
    Task CreateAsync(RefreshToken token);
    Task RevokeAsync(RefreshToken token);
}
```

**¿Por qué esta capa?**

- ✅ Define las reglas de negocio (entidades)
- ✅ Define contratos de acceso a datos (interfaces)
- ✅ No depende de ningún framework
- ✅ Fácil de testear
- ✅ Reutilizable en diferentes contextos

### 2. **Application Layer** (Capa de Aplicación)

**Ubicación**: `AtlanticOrders.Application/`

**Responsabilidad**: Contiene la lógica de negocio, servicios y transformación de datos.

**Componentes**:

#### DTOs (Data Transfer Objects)

```csharp
// LoginRequestDto
public class LoginRequestDto
{
    public string email { get; set; }
    public string password { get; set; }
}

// LoginResponseDto
public class LoginResponseDto
{
    public string Token { get; set; }
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; }
}

// RefreshTokenRequestDto
public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; }
}
```

**¿Por qué DTOs?**

- ✅ Decoupling: El API contrato no cambia si cambia la entidad interna
- ✅ Seguridad: No exponemos atributos internos sensibles
- ✅ Validación: Validamos datos de entrada antes de llegar a la lógica
- ✅ Mapeo: Transformamos entre formatos según necesidad

#### Services

```csharp
// AuthenticationProvider.cs
public interface IAuthenticationProvider
{
    Task<bool> ValidarCredencialesAsync(string email, string password);
}

public class AuthenticationProvider : IAuthenticationProvider
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    
    public async Task<bool> ValidarCredencialesAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email.ToLower());
        
        if (user == null || !user.IsActive)
            return false;
            
        return _passwordHasher.VerifyPassword(password, user.PasswordHash);
    }
}
```

#### Validators (FluentValidation)

```csharp
public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.email)
            .NotEmpty()
            .EmailAddress();
            
        RuleFor(x => x.password)
            .NotEmpty()
            .MinimumLength(6);
    }
}
```

#### Mappings (AutoMapper)

```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<CreateUserDto, User>();
    }
}
```

**¿Por qué esta capa?**

- ✅ Encapsula lógica de negocio
- ✅ Independiente de tecnología de persistencia
- ✅ Fácil de testear unitariamente
- ✅ Reutilizable en múltiples endpoints

### 3. **Infrastructure Layer** (Capa de Infraestructura)

**Ubicación**: `AtlanticOrders.Infrastructure/`

**Responsabilidad**: Implementaciones concretas de acceso a datos, servicios externos, seguridad.

**Componentes**:

#### Persistence (Entity Framework Core)

```
AtlanticOrdersDbContext.cs
├── OnModelCreating()     // Configuración de entidades
├── Fluent API           // Índices, relaciones, restricciones
└── DbSet<T>             // Acceso a tablas
```

#### Security

```
PasswordHasher.cs (BCrypt)
├── HashPassword(password)
└── VerifyPassword(password, hash)

JwtTokenProvider.cs
├── GenerarToken(email, role)
├── GenerarRefreshToken()
└── Validar secretkey

AuthenticationProvider.cs
├── ValidarCredencialesAsync()
└── Logging de intentos fallidos

RateLimitService.cs
├── CheckRateLimitAsync(ip)
└── Límite: 5 requests/min/IP
```

#### Repositories (Implementación)

```csharp
public class UserRepository : IUserRepository
{
    private readonly AtlanticOrdersDbContext _context;
    
    public async Task<User> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLower());
    }
}
```

**¿Por qué esta capa?**

- ✅ Aísla detalles de tecnología
- ✅ Implementa repositorio pattern
- ✅ Centraliza configuración de EF Core
- ✅ Fácil de switchear base de datos

### 4. **Presentation Layer** (Capa de Presentación)

**Ubicación**: `AtlanticOrders.Api/`

**Responsabilidad**: Recibe peticiones HTTP, delega a aplicación, retorna respuestas.

**Componentes**:

#### Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        // 1. Validar entrada (automático por FluentValidation)
        // 2. Llamar a service de autenticación
        // 3. Generar tokens
        // 4. Retornar respuesta
    }
}
```

#### Middleware

```csharp
public class GlobalExceptionHandlingMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Centralizar manejo de excepciones
            // Loguear
            // Retornar error estructurado
        }
    }
}
```

**¿Por qué esta capa?**

- ✅ Punto de entrada único
- ✅ Manejo de HTTP (headers, status codes, etc)
- ✅ Separada de lógica de negocio

## Patrones de Diseño

### 1. **Repository Pattern**

Abstrae el acceso a datos:

```csharp
// La aplicación NO sabe si estamos usando SQL Server o MongoDB
var user = await _userRepository.GetByEmailAsync("test@example.com");

// El repositorio sí lo sabe
public class UserRepository : IUserRepository
{
    // Implementación específica de SQL Server
}
```

**Ventajas**:
- ✅ Fácil cambiar base de datos
- ✅ Fácil hacer mocks para tests
- ✅ Centraliza queries

### 2. **Dependency Injection (DI)**

```csharp
// En Program.cs
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthenticationProvider, AuthenticationProvider>();

// En AuthController
public AuthController(IAuthenticationProvider authProvider)
{
    // El container inyecta la implementación
}
```

**Ventajas**:
- ✅ Desacoplamiento
- ✅ Testeable
- ✅ Configuración centralizada

### 3. **DTO Pattern**

Separación entre modelo interno y API:

```csharp
// Entidad interna (Dominio)
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }    // Nunca en API
    public DateTime CreatedAt { get; set; }
}

// DTO para API
public class UserDto
{
    public string Email { get; set; }
    public string FullName { get; set; }
    // Sin PasswordHash, sin Id, sin timestamps
}
```

**Ventajas**:
- ✅ Seguridad
- ✅ Control de versioning de API
- ✅ Validación

### 4. **Service Layer Pattern**

Encapsula lógica de negocio:

```csharp
public interface IAuthenticationService
{
    Task<LoginResponse> LoginAsync(string email, string password);
    Task<bool> ValidateTokenAsync(string token);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IAuthenticationProvider _authProvider;
    private readonly ITokenProvider _tokenProvider;
    
    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        // Orquesta múltiples componentes
    }
}
```

**Ventajas**:
- ✅ Reutilización
- ✅ Testeable
- ✅ Mantenible

### 5. **Factory Pattern** (Token Generation)

```csharp
public class JwtTokenProvider : ITokenProvider
{
    public TokenResult GenerarToken(string email, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, email),
                new Claim(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new TokenResult
        {
            Token = tokenHandler.WriteToken(token),
            ExpiresIn = _expirationMinutes * 60
        };
    }
}
```

## Tecnologías Utilizadas

### Core Framework

| Tecnología | Versión | Propósito |
|-----------|---------|----------|
| .NET | 8.0 | Runtime |
| ASP.NET Core | 8.0 | Framework web |
| C# | 12 | Lenguaje |

### Data Access

| Tecnología | Versión | Propósito |
|-----------|---------|----------|
| Entity Framework Core | 8.0 | ORM |
| SQL Server | 2019+ | BD (producción) |
| In-Memory DB | EF Core | BD (desarrollo) |

### Authentication & Security

| Tecnología | Versión | Propósito |
|-----------|---------|----------|
| JWT (JSON Web Tokens) | - | Autenticación stateless |
| BCrypt.Net-Core | 1.6+ | Hash de contraseñas |
| IdentityModel | 6.0+ | Token validation |

### Validation & Mapping

| Tecnología | Versión | Propósito |
|-----------|---------|----------|
| FluentValidation | 11.0+ | Validación fluida |
| AutoMapper | 12.0+ | Mapeo de objetos |

### Logging & Monitoring

| Tecnología | Versión | Propósito |
|-----------|---------|----------|
| Serilog | 3.0+ | Logging estructurado |
| Microsoft.Extensions.Logging | 8.0 | Logging nativo |

### Containerization

| Tecnología | Versión | Propósito |
|-----------|---------|----------|
| Docker | Latest | Containerización |
| Docker Compose | 1.29+ | Orquestación local |

## Flujos de Datos

### Flujo de Login

```
1. Usuario submite credenciales
   POST /api/auth/login
   {
     "email": "test@example.com",
     "password": "Test@123456"
   }

2. AuthController recibe petición
   └─> ValidarDeserialization (JSON)
   └─> ValidarSchema (FluentValidation)

3. LoginAction
   └─> IAuthenticationProvider.ValidarCredencialesAsync()
       ├─> IUserRepository.GetByEmailAsync()
       │   └─> Entity Framework Core -> SQL query
       ├─> IPasswordHasher.VerifyPassword()
       │   └─> BCrypt verification
       └─> Return bool

4. Si credenciales válidas
   ├─> ITokenProvider.GenerarToken()
   │   └─> JWT generation
   ├─> ITokenProvider.GenerarRefreshToken()
   │   └─> Secure random token
   └─> IRefreshTokenRepository.CreateAsync()
       └─> Store in database

5. Respuesta
   ├─> Set HttpOnly cookies
   │   └─> X-Access-Token
   │   └─> X-Refresh-Token
   └─> Retornar LoginResponseDto
       {
         "token": "eyJ...",
         "expiresIn": 3599,
         "refreshToken": "..."
       }
```

### Flujo de Authenticated Request

```
1. Cliente envía request con Authorization header
   GET /api/protected
   Authorization: Bearer eyJ...

2. Middleware JWT
   ├─> Extraer token del header (o cookie)
   ├─> ValidarSignatura
   │   └─> HMAC-SHA256 verification
   ├─> Validar expiration
   └─> Validar claims (issuer, audience)

3. Si válido, continuar con request
   └─> User context set
   └─> Principal con claims

4. Si inválido
   └─> Retornar 401 Unauthorized
```

## Diagrama de Componentes

```
┌───────────────────────────────────────────────────────────────────┐
│                         HTTP Request                              │
└────────────────────┬────────────────────────────────────────────────┘
                     │
                     ▼
        ┌────────────────────────┐
        │   AuthController       │
        │  (Presentation Layer)  │
        └────────────┬───────────┘
                     │
                     ▼
    ┌────────────────────────────────────┐
    │  AuthenticationProvider            │
    │  (Application Layer)               │
    │  - ValidarCredenciales()           │
    │  - Login orchestration             │
    └────────────┬───────────────────────┘
                 │
    ┌────────────┴──────────────┬──────────────────────┐
    │                           │                      │
    ▼                           ▼                      ▼
┌─────────────────┐   ┌──────────────────┐   ┌──────────────────┐
│ UserRepository  │   │ PasswordHasher   │   │ TokenProvider    │
│ (Infrastructure)│   │ (Infrastructure) │   │ (Infrastructure) │
└────────┬────────┘   └──────────────────┘   └──────────────────┘
         │                                                │
         ▼                                                │
    ┌─────────────────────────┐                         │
    │  DbContext              │                         │
    │  (EF Core)              │                         │
    └────────┬────────────────┘                         │
             │                                          │
             ▼                                          │
    ┌─────────────────────────┐                         │
    │  SQL Server / Memory DB │                         │
    └─────────────────────────┘                         │
                                                        │
                                                        ▼
                                            ┌──────────────────────┐
                                            │ JWT Token Response   │
                                            │ + HttpOnly Cookies   │
                                            └──────────────────────┘
```

## Escalabilidad

### Horizontal Scaling

```
Load Balancer
│
├── API Instance 1 (Docker Container)
│   └── In-Memory DB / Connection to shared SQL
│
├── API Instance 2 (Docker Container)
│   └── In-Memory DB / Connection to shared SQL
│
└── API Instance 3 (Docker Container)
    └── In-Memory DB / Connection to shared SQL

SQL Server (Shared)
└── Single source of truth for data
```

### Database Scaling

```
Application ──> Connection Pool (8-20 connections)
                        │
                        ▼
                SQL Server (Master)
                /    |     \
         Read 1  Read 2  Write
```

## Performance

### Caching (Puede implementarse)

```csharp
public class CachedUserRepository : IUserRepository
{
    private readonly IUserRepository _innerRepository;
    private readonly IMemoryCache _cache;
    
    public async Task<User> GetByEmailAsync(string email)
    {
        var cacheKey = $"user_{email}";
        if (_cache.TryGetValue(cacheKey, out User cached))
            return cached;
            
        var user = await _innerRepository.GetByEmailAsync(email);
        _cache.Set(cacheKey, user, TimeSpan.FromHours(1));
        return user;
    }
}
```

## Seguridad en Capas

```
┌─────────────────────────────────────┐
│ HTTPS (TLS 1.2+)                    │ ◄─ Network layer
├─────────────────────────────────────┤
│ Input Validation (FluentValidation) │ ◄─ Application layer
├─────────────────────────────────────┤
│ JWT Authentication                  │ ◄─ API layer
├─────────────────────────────────────┤
│ Authorization (Claims-based)        │ ◄─ Application layer
├─────────────────────────────────────┤
│ BCrypt Password Hashing             │ ◄─ Infrastructure layer
├─────────────────────────────────────┤
│ Rate Limiting (per IP)              │ ◄─ Middleware layer
├─────────────────────────────────────┤
│ CORS (Whitelist origins)            │ ◄─ Middleware layer
├─────────────────────────────────────┤
│ HttpOnly Secure Cookies             │ ◄─ HTTP layer
└─────────────────────────────────────┘
```

## Testabilidad

### Unit Testing Example

```csharp
[TestClass]
public class AuthenticationProviderTests
{
    private Mock<IUserRepository> _mockUserRepository;
    private Mock<IPasswordHasher> _mockPasswordHasher;
    private AuthenticationProvider _provider;
    
    [TestInitialize]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _provider = new AuthenticationProvider(
            _mockUserRepository.Object,
            _mockPasswordHasher.Object);
    }
    
    [TestMethod]
    public async Task LoginAsync_WithValidCredentials_ReturnsTrue()
    {
        // Arrange
        var user = new User { Email = "test@example.com", IsActive = true };
        _mockUserRepository
            .Setup(r => r.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        _mockPasswordHasher
            .Setup(h => h.VerifyPassword("Test@123", user.PasswordHash))
            .Returns(true);
        
        // Act
        var result = await _provider.ValidarCredencialesAsync("test@example.com", "Test@123");
        
        // Assert
        Assert.IsTrue(result);
    }
}
```

## Mantenimiento

### Logging

```csharp
private readonly ILogger<AuthController> _logger;

public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
{
    _logger.LogInformation("Login attempt for user: {email}", request.email);
    
    try
    {
        var result = await _authenticationProvider.ValidarCredencialesAsync(...);
        if (!result)
        {
            _logger.LogWarning("Login failed for user: {email}", request.email);
            return Unauthorized();
        }
        
        _logger.LogInformation("Login successful for user: {email}", request.email);
        return Ok(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during login for user: {email}", request.email);
        throw;
    }
}
```

---

**Última actualización**: Marzo 5, 2026
**Versión**: 1.0
