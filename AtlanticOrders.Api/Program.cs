using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using System.Text;
using AtlanticOrders.Infrastructure.Persistence;
using AtlanticOrders.Infrastructure.Repositories;
using AtlanticOrders.Domain.Repositories;
using AtlanticOrders.Application.Services;
using AtlanticOrders.Infrastructure.Security;
using AtlanticOrders.Application.Mappings;
using AtlanticOrders.Api.Middleware;
using AtlanticOrders.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURACIÓN DE SERVICIOS ====================

// 1. Configurar base de datos
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
if (useInMemory)
{
    builder.Services.AddDbContext<AtlanticOrdersDbContext>(options =>
        options.UseInMemoryDatabase("AtlanticOrdersDb"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<AtlanticOrdersDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// 2. Configurar AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// 3. Configurar validaciones con FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// 4. Configurar servicios de aplicación
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IPedidoService, PedidoService>();

// 5. Configurar servicios de autenticación y seguridad
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IAuthenticationProvider, AuthenticationProvider>();

// 6. Configurar Rate Limiting
builder.Services.AddSingleton<IRateLimitService, InMemoryRateLimitService>();

// 7. Configurar seguridad JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");

// Leer SecretKey desde múltiples fuentes (en orden de prioridad):
// 1. Variable de entorno JWT_SECRET_KEY (producción)
// 2. User Secrets (desarrollo local)
// 3. appsettings.json (fallback, pero NO debe contener secreto)
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException(
        "JWT:SecretKey no está configurada. " +
        "Usa: dotnet user-secrets set \"Jwt:SecretKey\" \"your-secret-key-here\" " +
        "O establece la variable de entorno JWT_SECRET_KEY");

var issuer = jwtSettings["Issuer"] ?? "AtlanticOrdersApi";
var audience = jwtSettings["Audience"] ?? "AtlanticOrdersApp";
var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

// Validar que la clave tenga al menos 32 caracteres (256 bits para HS256)
if (secretKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT:SecretKey debe tener al menos 32 caracteres para HS256. " +
        "Genera una clave segura con: openssl rand -base64 32");
}

builder.Services.AddScoped<ITokenProvider>(sp =>
    new JwtTokenProvider(secretKey, issuer, audience, expirationMinutes));

// 8. Configurar autenticación JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.FromSeconds(10) // Pequeño margen para sincronización de relojes
        };

        // Leer token desde HttpOnly cookies si no está en Authorization header
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Primero intentar obtener del header Authorization (estándar)
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

                // Si no está en header, intentar obtener de HttpOnly cookie
                if (string.IsNullOrEmpty(token) && context.Request.Cookies.TryGetValue("X-Access-Token", out var cookieToken))
                {
                    token = cookieToken;
                }

                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

// 9. Configurar CORS
var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? new[]
{
    "http://localhost:3000",    // React por defecto
    "http://localhost:5173",    // Vite por defecto
    "http://localhost:4200",    // Angular por defecto
    "http://localhost:5000",    // Otros
    "http://localhost:5001"     // Otros con HTTPS
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
    
    // Política CORS por defecto para todos los endpoints
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// 10. Agregar controladores y swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();

// ==================== CONFIGURACIÓN DE LA APLICACIÓN ====================

var app = builder.Build();

// Middleware global de excepciones
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Middleware de rate limiting
app.UseRateLimiting();

// Swagger en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

// ==================== MIGRACIONES Y INICIALIZACIÓN ====================

// Aplicar migraciones automáticamente y sembrar datos de prueba
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AtlanticOrdersDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        
        // Crear la base de datos si no existe
        await dbContext.Database.EnsureCreatedAsync();
        
        // Alternatively, usar migraciones:
        // await dbContext.Database.MigrateAsync();
        
        // Sembrar datos de prueba si no existen usuarios
        if (!dbContext.Users.Any())
        {
            var logger = app.Services.GetService<ILogger<Program>>();
            logger?.LogInformation("Sembrando datos de prueba en la base de datos...");
            
            // Crear usuarios de prueba
            var testUsers = new List<User>
            {
                new User
                {
                    Email = "test@example.com",
                    PasswordHash = passwordHasher.HashPassword("Test@123456"),
                    FullName = "Test User",
                    Role = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Email = "admin@example.com",
                    PasswordHash = passwordHasher.HashPassword("Admin@123456"),
                    FullName = "Admin User",
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Email = "demo@example.com",
                    PasswordHash = passwordHasher.HashPassword("Demo@123456"),
                    FullName = "Demo User",
                    Role = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
            
            dbContext.Users.AddRange(testUsers);
            await dbContext.SaveChangesAsync();
            
            logger?.LogInformation("✓ {count} usuarios de prueba creados exitosamente", testUsers.Count);
            logger?.LogInformation("  - test@example.com / Test@123456");
            logger?.LogInformation("  - admin@example.com / Admin@123456");
            logger?.LogInformation("  - demo@example.com / Demo@123456");
        }
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetService<ILogger<Program>>();
    logger?.LogError(ex, "Error al inicializar la base de datos. La aplicación seguirá ejecutándose, pero sin persistencia de datos.");
}

app.Run();
