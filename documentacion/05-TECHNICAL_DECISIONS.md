# Atlantic Orders Backend - Decisiones Técnicas y Justificación Arquitectónica

## Descripción General

Este documento explica el "por qué" detrás de cada decisión arquitectónica importante en el Atlantic Orders Backend. Estas decisiones se basan en mejores prácticas de la industria, requisitos de escalabilidad, consideraciones de seguridad y mantenibilidad a largo plazo. Este documento está diseñado para entrevistas técnicas, revisiones de código y discusiones arquitectónicas.

---

## Tabla de Contenidos

1. [Patrón Arquitectónico: Arquitectura Limpia](#1-arquitectura-limpia)
2. [Estrategia de Autenticación](#2-estrategia-de-autenticación)
3. [Limitación de Velocidad y Protección contra DDoS](#3-limitación-de-velocidad--protección-contra-ddos)
4. [Seguridad de Contraseñas](#4-seguridad-de-contraseñas)
5. [Gestión de Tokens](#5-gestión-de-tokens)
6. [Patrón de Repositorio](#6-patrón-de-repositorio)
7. [Objetos de Transferencia de Datos (DTOs)](#7-objetos-de-transferencia-de-datos-dtos)
8. [Entity Framework Core](#8-entity-framework-core)
9. [Inyección de Dependencias](#9-inyección-de-dependencias)
10. [Manejo de Errores y Registro](#10-manejo-de-errores-y-registro)
11. [CORS y Encabezados de Seguridad](#11-cors--encabezados-de-seguridad)
12. [Estrategia de Pruebas](#12-estrategia-de-pruebas)

---

## 1. Arquitectura Limpia

### Decisión: Implementar Arquitectura Limpia (Patrón de 4 Capas)

```
┌─────────────────────────────────────┐
│   Presentación (Controladores API)  │
├─────────────────────────────────────┤
│   Aplicación (Servicios, DTOs)      │
├─────────────────────────────────────┤
│   Dominio (Lógica, Entidades)       │
├─────────────────────────────────────┤
│   Infraestructura (Datos, Seguridad)│
└─────────────────────────────────────┘
```

### ¿Por qué Arquitectura Limpia?

**Alternativas Consideradas:**
- **Arquitectura N-Tier**: Todas las capas dependen entre sí, creando acoplamiento fuerte
- **Arquitectura Estratificada**: Similar a N-tier pero con menos reglas de dependencia
- **Arquitectura Monolítica**: Todo en una capa (difícil de probar, mantener, escalar)

**Razones para Arquitectura Limpia:**

1. **Independencia de Frameworks**
   - La lógica empresarial (capa de Dominio) no tiene dependencias de ASP.NET Core, Entity Framework u otras librerías
   - Si necesitamos reemplazar Entity Framework Core con Dapper o NHibernate, solo cambiamos Infraestructura
   - Más fácil adoptar nuevas tecnologías sin reescribir la lógica empresarial

2. **Testabilidad**
   ```csharp
   // Fácil de probar porque AuthenticationService depende de abstracciones
   public class AuthenticationServiceTests
   {
       private Mock<IUserRepository> _mockUserRepository;
       private Mock<IPasswordHasher> _mockPasswordHasher;
       private AuthenticationService _service;
       
       [SetUp]
       public void Setup()
       {
           _mockUserRepository = new Mock<IUserRepository>();
           _mockPasswordHasher = new Mock<IPasswordHasher>();
           _service = new AuthenticationService(_mockUserRepository.Object, _mockPasswordHasher.Object);
       }
       
       [Test]
       public async Task Login_WithValidCredentials_ReturnsToken()
       {
           // Arrange
           var user = new User { Email = "test@example.com", PasswordHash = "hashed" };
           _mockUserRepository.Setup(r => r.GetByEmailAsync("test@example.com"))
               .ReturnsAsync(user);
           _mockPasswordHasher.Setup(p => p.VerifyPassword("password", "hashed"))
               .Returns(true);
           
           // Act
           var result = await _service.AuthenticateAsync("test@example.com", "password");
           
           // Assert
           Assert.IsNotNull(result.AccessToken);
       }
   }
   ```

3. **Separación de Responsabilidades**
   - La capa de Dominio contiene solo reglas empresariales
   - La capa de Aplicación orquesta la lógica del dominio
   - Infraestructura maneja detalles de persistencia
   - Los Controladores solo manejan preocupaciones HTTP
   - Cada capa tiene una única responsabilidad

4. **Reutilización de Lógica Empresarial**
   - La misma lógica de autenticación se puede usar vía API REST, gRPC o colas de mensajes
   - Las reglas empresariales no están vinculadas a HTTP
   - Más fácil agregar nuevas interfaces (aplicación de consola, trabajo en segundo plano, etc.)

5. **Escalabilidad del Equipo**
   - Los equipos pueden trabajar independientemente en diferentes capas
   - Contratos claros entre capas (interfaces)
   - Menos conflictos de fusión y problemas de acoplamiento

### Compensaciones:

| Aspecto | Beneficio | Costo |
|---------|---------|------|
| **Abstracción** | Flexible, comprobable, mantenible | Más archivos, más interfaces |
| **Independencia** | Fácil de cambiar implementaciones | Mayor sobrecarga de configuración inicial |
| **Testabilidad** | Se pueden burlar las dependencias fácilmente | Requiere disciplina del equipo |
| **Rendimiento** | Sin sobrecarga inherente si se hace correctamente | Potencial para sobre-ingeniería |

### Cuándo NO Usar:
- Aplicaciones CRUD simples con <5 entidades
- Prototipo/MVP donde velocidad > mantenibilidad
- Proyectos en solitario que nunca crecerán
- Tableros de solo lectura con lógica mínima

---

## 2. Estrategia de Autenticación

### Decisión: Autenticación sin Estado con JWT

### ¿Por qué NO Sesiones (Enfoque Tradicional)?

**Problemas de Sesiones:**
```
Cliente                             Servidor
   |                                  |
   |-- POST /login ----------------->|
   |      (credenciales)               |
   |                                  |
   |<-- Set-Cookie: SessionID --------|
   |     (servidor almacena sesión)   |
   |                                  |
   |-- GET /orders ----------------->|
   |      (envía SessionID)           |
   |      (servidor busca sesión)     |
   |<-- 200 OK ----------------------|
```

**Problemas con Sesiones:**
1. **Servidor con Estado**: El servidor debe almacenar y recuperar sesiones para cada solicitud
   - Requiere caché compartida (Redis, Memcached) para sistemas distribuidos
   - Consulta de base de datos por solicitud afecta la latencia
   - Complejidad de replicación de sesiones en sistemas con carga balanceada

2. **Problemas de Escalabilidad**:
   - No se puede escalar horizontalmente fácilmente (afinidad de sesión requerida)
   - Los datos de sesión deben compartirse entre servidores
   - La limpieza/expiración de sesiones se vuelve compleja

3. **Problemas con Móvil/API**:
   - Las cookies no funcionan bien con aplicaciones móviles
   - Complicaciones de CORS con credenciales
   - Gestión de cookies en diferentes clientes

### ¿Por qué JWT (Tokens Web JSON)?

```
Cliente                             Servidor
   |                                  |
   |-- POST /login ----------------->|
   |      (credenciales)               |
   |                                  |
   |<-- JWT Token -------------------|
   |     (autónomo, sin almacenamiento)|
   |                                  |
   |-- GET /orders ----------------->|
   |      Authorization: Bearer JWT   |
   |      (servidor verifica firma)   |
   |<-- 200 OK ----------------------|
```

**Ventajas de JWT:**
1. **Sin Estado**: El servidor no necesita almacenar tokens
   - El servidor solo necesita la clave de firma (siempre disponible)
   - Puede verificar la legitimidad del token criptográficamente
   - No se necesitan búsquedas de base de datos

2. **Escalabilidad**:
   - Funciona perfectamente con equilibradores de carga y escalado horizontal
   - No se necesita replicación de sesiones
   - Cualquier servidor puede verificar cualquier token

3. **Contiene Información del Usuario**:
   ```csharp
   // Payload JWT contiene reclamaciones del usuario
   var claims = new List<Claim>
   {
       new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
       new Claim(ClaimTypes.Email, user.Email),
       new Claim(ClaimTypes.Role, user.Role.ToString())
   };
   
   // El servidor puede leer información del usuario sin búsqueda de BD
   var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
   ```

4. **Amigable con Móviles**:
   - Funciona con cualquier cliente HTTP (aplicaciones móviles, SPAs, aplicaciones de escritorio)
   - No se requiere gestión de cookies
   - Formato estándar de encabezado Authorization

5. **Dominio Cruzado/CORS**:
   - Sin problemas de cookies con diferentes dominios
   - Funciona sin problemas con microservicios

### Estructura JWT:

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
│                                           │                                              │
│                                           │                                              │
Encabezado (Base64)                 Carga útil (Base64)                             Firma (HMAC)
{"alg": "HS256",                   {"sub": "1234567890",
 "typ": "JWT"}                      "name": "John Doe",
                                    "iat": 1516239022}
```

**Mecanismo de Seguridad**:
```csharp
// El servidor firma el token
var tokenHandler = new JwtSecurityTokenHandler();
var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);
var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(claims),
    Expires = DateTime.UtcNow.AddMinutes(15),
    SigningCredentials = new SigningCredentials(
        new SymmetricSecurityKey(key), 
        SecurityAlgorithms.HmacSha256Signature)
});

// El cliente envía el token nuevamente, el servidor verifica la firma
// Si la firma es inválida o fue manipulada, la verificación falla
// ¡Sin búsqueda de base de datos necesaria!
```

### Compensaciones:

| Aspecto | JWT | Sesiones |
|--------|-----|----------|
| **Escalabilidad** | ✅ Excelente | ❌ Pobre (necesita estado compartido) |
| **Revocación de Token** | ⚠️ Requiere lista negra | ✅ Instantánea |
| **Tamaño de Token** | ⚠️ Más grande (incluye reclamaciones) | ✅ Más pequeño (solo ID) |
| **Complejidad** | ⚠️ Más complejo | ✅ Más simple |
| **Soporte Móvil** | ✅ Excelente | ❌ Pobre |
| **Sin Estado** | ✅ Sí | ❌ No |

---

## 3. Limitación de Velocidad y Protección contra DDoS

### Decisión: Implementar Limitación de Velocidad por IP (5 solicitudes/minuto para endpoints de autenticación)

### ¿Por qué Limitación de Velocidad?

**Amenazas de Seguridad sin Limitación de Velocidad:**

1. **Ataques de Fuerza Bruta**
   ```
   El atacante intenta millones de combinaciones de contraseña:
   POST /auth/login HTTP/1.1
   {"email": "user@example.com", "password": "attempt1"}
   {"email": "user@example.com", "password": "attempt2"}
   {"email": "user@example.com", "password": "attempt3"}
   ... (millones de intentos en segundos)
   ```

2. **Relleno de Credenciales**
   - El atacante usa contraseñas filtradas de otras brechas
   - Sin limitación de velocidad, puede probar miles de combinaciones email/contraseña

3. **Ataques DDoS**
   - El atacante envía un número masivo de solicitudes para sobrecargar el servidor
   - Los usuarios legítimos obtienen servicio no disponible

4. **Agotamiento de Recursos**
   - Cada solicitud consume CPU, memoria, conexiones de base de datos
   - El atacante puede agotar todos los recursos

### Estrategia de Implementación:

```csharp
public class RateLimitService
{
    private readonly ConcurrentDictionary<string, RateLimitInfo> _store = 
        new ConcurrentDictionary<string, RateLimitInfo>();

    public async Task<bool> IsAllowedAsync(string clientId, int maxRequests, int windowSeconds)
    {
        var key = clientId;
        var now = DateTimeOffset.UtcNow;
        
        if (_store.TryGetValue(key, out var info))
        {
            // La ventana expiró, reiniciar
            if (now - info.WindowStart > TimeSpan.FromSeconds(windowSeconds))
            {
                info.Count = 1;
                info.WindowStart = now;
                return true;
            }
            
            // Todavía en la ventana
            if (info.Count < maxRequests)
            {
                Interlocked.Increment(ref info.Count);
                return true;
            }
            
            // Límite excedido
            return false;
        }
        
        // Primera solicitud de esta IP
        _store.TryAdd(key, new RateLimitInfo 
        { 
            Count = 1, 
            WindowStart = now 
        });
        return true;
    }
}
```

### Configuración por Endpoint:

```csharp
// auth/login: Limitación agresiva de velocidad (5 req/min)
// Previene ataques de fuerza bruta
[HttpPost("login")]
[RateLimit(maxRequests: 5, windowSeconds: 60)]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    // Implementación...
}

// auth/refresh: Limitación moderada de velocidad (30 req/min)
// Las aplicaciones legítimas actualizan tokens periódicamente
[HttpPost("refresh")]
[RateLimit(maxRequests: 30, windowSeconds: 60)]
public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
{
    // Implementación...
}

// api/orders: Límite más alto (100 req/min)
// El uso normal de API no debería alcanzar esto
[HttpGet("orders")]
[RateLimit(maxRequests: 100, windowSeconds: 60)]
public async Task<IActionResult> GetOrders()
{
    // Implementación...
}
```

### Defensa Multicapa:

```
Capa 1: Limitación de Velocidad (Por IP)
├─ Detiene al atacante único
└─ Ralentiza el ataque de fuerza bruta

Capa 2: Bloqueo de Cuenta
├─ Bloquea cuenta después de N intentos fallidos de inicio de sesión
└─ Requiere verificación de correo electrónico para desbloquear

Capa 3: WAF (Firewall de Aplicación Web)
├─ Bloqueo basado en IP para actores malos conocidos
└─ Detección basada en patrones

Capa 4: Servicio de Protección DDoS
├─ Cloudflare, AWS Shield, etc.
└─ Absorbe picos masivos de tráfico
```

### Compensaciones:

| Enfoque | Protección | Impacto del Usuario |
|----------|-----------|-------------|
| **Basado en IP** | Bueno para atacante único | Los usuarios de VPN podrían bloquearse |
| **Basado en Cuenta** | Bueno para protección de cuenta | Requiere autenticación primero |
| **Basado en Dispositivo** | Bueno para ataques dirigidos | Complejo de implementar |
| **Comportamental** | Mejor en general | Alta tasa de falsos positivos |

---

## 4. Seguridad de Contraseñas

### Decisión: Usar BCrypt con 12 Rondas de Salt

### ¿Por qué BCrypt sobre Otros Algoritmos de Hash?

**Algoritmos Comparados:**

```
╔════════════════════════════════════════════════════════════╗
║ Algoritmo   │ Velocidad  │ Salted │ Adaptativo │ GPU-Seguro   ║
╠════════════════════════════════════════════════════════════╣
║ SHA-256     │ Muy Rápido │ Manual │ No         │ ❌ Débil   ║
║ SHA-512     │ Muy Rápido │ Manual │ No         │ ❌ Débil   ║
║ MD5         │ Muy Rápido │ No     │ No         │ ❌ Roto  ║
║ PBKDF2      │ Lento      │ Sí     │ Sí         │ ✅ Bueno   ║
║ Bcrypt      │ Lento      │ Sí     │ Sí         │ ✅ Excelente ║
║ Scrypt      │ Muy Lento  │ Sí     │ Sí         │ ✅ Excelente ║
║ Argon2      │ Lento      │ Sí     │ Sí         │ ✅ Mejor   ║
╚════════════════════════════════════════════════════════════╝
```

### ¿Por qué NO SHA-256?

```csharp
// ❌ INCORRECTO: El hash simple NO es seguridad de contraseña
var hash = SHA256.ComputeHash(Encoding.UTF8.GetBytes(password));

// Problemas:
// 1. RÁPIDO - El atacante puede probar miles de millones de contraseñas por segundo
// 2. SIN SALT - Las tablas arcoíris funcionan perfectamente
// 3. NO ADAPTATIVO - No se puede aumentar la dificultad con el tiempo

// Real: Romper contraseñas de 8 caracteres: <1 segundo con GPU moderna
```

### ¿Por qué BCrypt?

```csharp
// ✅ CORRECTO: BCrypt es lento y adaptativo
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

// Propiedades de Seguridad:
// 1. LENTO - Intencionalmente toma 100-200ms por hash
// 2. SALTED - Salt aleatorio incluido, hashes únicos incluso para la misma contraseña
// 3. ADAPTATIVO - El factor de trabajo puede aumentar a medida que las computadoras se vuelven más rápidas
```

**Cómo Funciona BCrypt:**

```
Contraseña: "MyPassword123"
├─ Generar salt aleatorio (16 bytes)
│  └─ Salt: $2b$12$abcdefghijklmnop
│
├─ Ejecutar derivación de clave cara (2^workfactor iteraciones)
│  └─ 4096 iteraciones con workfactor=12
│
└─ Salida: $2b$12$abcdefghijklmnop.G7TLC8PXD6QLfudYIHeC3P7YCfnWEhQO

Estructura: $2b$ 12 $ salt(22) hash(31)
            versión  factor-trabajo
```

**Puntos de Referencia:**

```
Algoritmo         Tiempo por hash    Contraseñas/segundo
─────────────────────────────────────────────────────────
SHA-256           0.000001 seg       1,000,000+
BCrypt (rondas=12) 0.100 seg          10
SHA-512           0.000003 seg       300,000+
BCrypt (rondas=14) 0.400 seg          2.5
Argon2            0.050 seg           20
```

**Impacto del Ataque:**

```
El atacante intenta romper 100,000 contraseñas:

SHA-256:
├─ Tiempo: 0.1 segundos (GPU)
└─ Resultado: Todas las contraseñas rotas fácilmente

BCrypt (rondas=12):
├─ Tiempo: 10,000 segundos (¡278 horas!)
└─ Resultado: Impracticable atacar

Conclusión: El hash lento = Mejor defensa contra ataques sin conexión
```

### Implementación:

```csharp
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // 100-200ms por hash
    
    public string HashPassword(string password)
    {
        // BCrypt genera automáticamente el salt
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            // Comparación constante en tiempo (previene ataques de temporización)
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (SaltParseException)
        {
            return false; // Formato de hash inválido
        }
    }
}
```

### ¿Por qué Factor de Trabajo = 12?

```csharp
// Factor de Trabajo = log2(iteraciones)
// Factor de Trabajo = 12 → 2^12 = 4,096 iteraciones

// Recomendaciones por año:
// 2010: Factor de Trabajo = 10 (1,024 iteraciones) - 10ms
// 2015: Factor de Trabajo = 12 (4,096 iteraciones) - 100ms
// 2020: Factor de Trabajo = 13 (8,192 iteraciones) - 200ms
// 2025: Considerar Factor de Trabajo = 14 (16,384 iteraciones) - 400ms

// Compensación: Seguridad vs Experiencia del Usuario
// Demasiado lento: Los usuarios esperan 1+ segundo para iniciar sesión ❌
// Demasiado rápido: El atacante puede romper contraseñas rápidamente ❌
// Justo correcto: 100-200ms (el usuario no lo nota, el atacante no puede romper) ✅
```

---

## 5. Gestión de Tokens

### Decisión: Tokens de Acceso de Corta Duración + Tokens de Actualización de Larga Duración + Rotación de Tokens

### ¿Por qué NO un Token Único de Larga Duración?

**Riesgo de Seguridad:**
```
El usuario inicia sesión una vez:
├─ Obtiene token válido por 30 días
├─ Token almacenado en dispositivo/navegador
│  └─ Si el dispositivo se roba/compromete:
│     Token es válido por 30 días más
│     El atacante tiene acceso completo
│
└─ Daño: 30 días de acceso no autorizado posible

Si el token se filtra (XSS, robo de dispositivo, etc):
└─ El atacante tiene una ventana larga para usarlo
```

### La Arquitectura de Token de Actualización:

```
┌──────────────┐
│   Cliente    │
└──────────────┘
       │
       │ 1. POST /login (credenciales)
       ▼
┌──────────────────────────────────────────────┐
│  Servidor - Autenticar e Emitir Tokens      │
├──────────────────────────────────────────────┤
│ ✅ Validar credenciales                     │
│ ✅ Crear AccessToken (expiración 15 min)   │
│ ✅ Crear RefreshToken (expiración 7 días)  │
│ ✅ Almacenar RefreshToken en BD            │
│ ✅ Devolver ambos tokens al cliente        │
└──────────────────────────────────────────────┘
       │
       │ 2. Devolver: {accessToken, refreshToken}
       ▼
┌──────────────────────────────────────────────┐
│  Almacenamiento del Cliente                 │
├──────────────────────────────────────────────┤
│ • AccessToken: En memoria (se pierde al actualizar) │
│ • RefreshToken: Cookie HttpOnly            │
│   (No se puede acceder desde JavaScript)   │
└──────────────────────────────────────────────┘
       │
       │ 3. Usar AccessToken para solicitudes API
       │    GET /api/orders
       │    Authorization: Bearer {accessToken}
       ▼
┌──────────────────────────────────────────────┐
│  Servidor - Validar AccessToken            │
├──────────────────────────────────────────────┤
│ ✅ Verificar firma JWT                     │
│ ✅ Verificar expiración (15 min)           │
│ ✅ Extraer reclamaciones                   │
│ ❌ ¡Sin búsqueda de BD necesaria!          │
└──────────────────────────────────────────────┘
       │
       │ 4. AccessToken expira después de 15 min
       │    El cliente debe actualizar
       ▼
┌──────────────────────────────────────────────┐
│  Auto-Actualización del Cliente (Interceptor) │
├──────────────────────────────────────────────┤
│ // El interceptor de Axios detecta 401      │
│ POST /auth/refresh                          │
│ (RefreshToken enviado en Cookie)           │
└──────────────────────────────────────────────┘
       │
       │ 5. El servidor valida RefreshToken e emite nuevo AccessToken
       │    (Si el refresh token está en lista negra o inválido, falla auth)
       ▼
┌──────────────────────────────────────────────┐
│  Servidor - Validar y Rotar RefreshToken   │
├──────────────────────────────────────────────┤
│ ✅ Buscar RefreshToken en BD               │
│ ✅ Verificar que no esté en lista negra   │
│ ✅ Verificar expiración (7 días)          │
│ ✅ Crear NUEVO RefreshToken (rotación)   │
│ ✅ Invalidar RefreshToken ANTERIOR         │
│ ✅ Devolver nuevo AccessToken + nuevo RefreshToken│
└──────────────────────────────────────────────┘
```

### Beneficios de Esta Arquitectura:

```
Escenario 1: AccessToken se filtra (XSS o intercepción de red)
├─ Duración de vulnerabilidad: 15 minutos
├─ Ventana del atacante: 15 minutos
└─ Riesgo: Medio (Tiempo limitado para causar daño)

Escenario 2: RefreshToken se filtra
├─ Duración: 7 días
├─ Pero: El servidor registra el uso de tokens (puede detectar abuso)
├─ Y: Puede revocar todos los tokens del usuario (cerrar sesión en todos los dispositivos)
└─ Riesgo: Medio-Alto (Pero detectable y revocable)

Escenario 3: Ambos tokens se filtran (compromiso total de cuenta)
├─ El servidor detecta múltiples intentos de actualización desde diferentes IPs
├─ Desencadena alerta de seguridad
├─ Puede invalidar todos los tokens
└─ El usuario debe re-autenticarse
```

### Implementación de Código:

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    // 1. Validar credenciales
    var user = await _authService.AuthenticateAsync(request.Email, request.Password);
    if (user == null)
        return Unauthorized("Credenciales inválidas");

    // 2. Generar token de acceso de corta duración (15 min)
    var accessToken = _jwtTokenProvider.GenerateAccessToken(user);
    
    // 3. Generar token de actualización de larga duración (7 días)
    var refreshToken = _authService.GenerateRefreshToken(user.Id);
    
    // 4. Almacenar token de actualización en BD
    await _refreshTokenRepository.AddAsync(new RefreshToken
    {
        UserId = user.Id,
        Token = refreshToken.Token,
        ExpiresAt = refreshToken.ExpiresAt,
        IsRevoked = false,
        CreatedAt = DateTime.UtcNow,
        CreatedByIp = GetClientIpAddress()
    });

    // 5. Devolver tokens (RefreshToken como cookie HttpOnly)
    Response.Cookies.Append("refreshToken", refreshToken.Token, new CookieOptions
    {
        HttpOnly = true,      // No se puede acceder desde JavaScript (protección XSS)
        Secure = true,        // Solo HTTPS (protección MITM)
        SameSite = SameSiteMode.Strict, // Protección CSRF
        Expires = refreshToken.ExpiresAt
    });

    return Ok(new 
    { 
        accessToken = accessToken,
        refreshToken = refreshToken.Token // También devolver en cuerpo para aplicaciones móviles
    });
}

[HttpPost("refresh")]
[Authorize] // El usuario debe tener JWT válido (pero puede estar expirado)
public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
{
    // 1. Obtener token de actualización de cookie o cuerpo de solicitud
    var refreshTokenValue = request.RefreshToken ?? 
        Request.Cookies["refreshToken"];
    
    if (string.IsNullOrEmpty(refreshTokenValue))
        return BadRequest("Token de actualización requerido");

    // 2. Validar token de actualización
    var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshTokenValue);
    if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt <= DateTime.UtcNow)
        return Unauthorized("Token de actualización inválido");

    var user = await _userRepository.GetByIdAsync(storedToken.UserId);

    // 3. Generar nuevo token de acceso
    var newAccessToken = _jwtTokenProvider.GenerateAccessToken(user);
    
    // 4. Rotar token de actualización (invalidar antiguo, crear nuevo)
    storedToken.IsRevoked = true; // Poner en lista negra token antiguo
    await _refreshTokenRepository.UpdateAsync(storedToken);
    
    var newRefreshToken = _authService.GenerateRefreshToken(user.Id);
    await _refreshTokenRepository.AddAsync(newRefreshToken);

    // 5. Devolver nuevos tokens
    Response.Cookies.Append("refreshToken", newRefreshToken.Token, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = newRefreshToken.ExpiresAt
    });

    return Ok(new 
    { 
        accessToken = newAccessToken,
        refreshToken = newRefreshToken.Token
    });
}

[HttpPost("logout")]
[Authorize]
public async Task<IActionResult> Logout()
{
    // Invalidar todos los tokens de actualización de este usuario
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    await _refreshTokenRepository.RevokeAllByUserAsync(int.Parse(userId));
    
    // Borrar cookie de token de actualización
    Response.Cookies.Delete("refreshToken");
    
    return Ok("Sesión cerrada exitosamente");
}
```

### Tiempos de Expiración de Tokens - ¿Por Qué Estos Valores?

```
AccessToken: 15 minutos
├─ Suficientemente corto: Si se filtra, el daño es limitado
├─ Suficientemente largo: No causará solicitudes excesivas de actualización
├─ Típico: El estándar de la industria es 5-60 minutos
└─ Nuestra elección: 15 min equilibra seguridad y UX

RefreshToken: 7 días
├─ Suficientemente largo: No necesita re-iniciar sesión cada día
├─ Suficientemente corto: Limita el daño si se compromete
├─ Típico: El estándar de la industria es 7-30 días
└─ Nuestra elección: 7 días se alinea con expectativas de "recuérdame"

Cierre de Sesión: Inmediato
├─ Todos los tokens se ponen en lista negra
├─ Sin período de tolerancia
└─ Asegura revocación instantánea
```

### Compensaciones:

| Decisión | Beneficio | Costo |
|----------|---------|------|
| **AccessToken Corto** | Alta seguridad, exposición limitada | Más llamadas de actualización |
| **Rotación de RefreshToken** | Detecta robo de token, limita daño | Más escrituras en BD |
| **Cookies HttpOnly** | Protección XSS para RefreshToken | No se puede acceder desde JS |
| **JWT Sin Estado** | Escalabilidad, rendimiento | No se puede revocar en el medio |

---

## 6. Patrón de Repositorio

### Decisión: Usar Patrón de Repositorio con Inyección de Dependencias

### ¿Por qué Patrón de Repositorio?

**Sin Repositorio (Acceso Directo a Datos):**
```csharp
// ❌ ANTIPATRÓN: Acceso directo a BD en el servicio
public class OrderService
{
    private readonly AtlanticOrdersDbContext _context;

    public async Task<Order> GetOrderAsync(int id)
    {
        // El servicio toca Entity Framework directamente
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
        
        return order; // Devuelve entidad de Entity Framework
    }
    
    public async Task CreateOrderAsync(Order order)
    {
        // El servicio manipula DbContext directamente
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
    }
}

// Problemas:
// 1. Difícil de probar (no se puede burlar Entity Framework fácilmente)
// 2. Lógica empresarial acoplada a Entity Framework
// 3. Si cambiamos de EF a Dapper, reescribir todo el servicio
// 4. Múltiples servicios haciendo las mismas consultas = duplicación de código
// 5. Lógica de consultas dispersa en el código
```

**Con Patrón de Repositorio (Abstracción de Acceso a Datos):**

```csharp
// Interfaz en capa de Dominio
public interface IOrderRepository
{
    Task<Order> GetByIdAsync(int id);
    Task<IEnumerable<Order>> GetAllAsync();
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task DeleteAsync(int id);
}

// Implementación en capa de Infraestructura
public class OrderRepository : IOrderRepository
{
    private readonly AtlanticOrdersDbContext _context;

    public OrderRepository(AtlanticOrdersDbContext context)
    {
        _context = context;
    }

    public async Task<Order> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
    
    public async Task AddAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
    }
}

// El servicio depende de la abstracción, no de la implementación
public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<Order> GetOrderAsync(int id)
    {
        // El servicio no sabe DE DÓNDE vienen los datos
        return await _repository.GetByIdAsync(id);
    }
}

// Beneficios:
// 1. Fácil de probar (inyectar repositorio simulado)
// 2. Lógica empresarial independiente de la fuente de datos
// 3. Cambiar de EF a Dapper = solo cambiar repositorio
// 4. Lógica de consultas centralizada y reutilizable
// 5. Código de servicio enfocado en reglas empresariales
```

### Repositorio vs Objetos de Consulta vs CQRS:

```
┌─────────────────────────────────────────────────────────┐
│ Patrón          │ Complejidad │ Testing │ Cuándo Usar   │
├─────────────────────────────────────────────────────────┤
│ Repositorio     │ Medio       │ Fácil   │ Apps CRUD     │
├─────────────────────────────────────────────────────────┤
│ Objetos Consulta│ Medio       │ Medio   │ Consultas complejas │
├─────────────────────────────────────────────────────────┤
│ CQRS            │ Alta        │ Difícil │ Event sourcing│
├─────────────────────────────────────────────────────────┤
│ EF Directo      │ Baja        │ Difícil │ Nunca         │
└─────────────────────────────────────────────────────────┘
```

**Nuestra Elección: Patrón de Repositorio**
- No demasiado complejo (principalmente operaciones CRUD)
- Fácil de probar (repositorios simulados)
- Buena separación de responsabilidades
- Escalable (puede agregar objetos de consulta después si es necesario)

### Implementación de Repositorio en Atlantic Orders:

```csharp
// Interfaz de capa de Dominio
public interface IUserRepository
{
    Task<User> GetByIdAsync(int id);
    Task<User> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
}

// Implementación de capa de Infraestructura
public class UserRepository : IUserRepository
{
    private readonly AtlanticOrdersDbContext _context;

    public UserRepository(AtlanticOrdersDbContext context)
    {
        _context = context;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}

// Servicio de capa de Aplicación (no conoce sobre EF)
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthenticationService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<User> AuthenticateAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return null;

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
            return null;

        return user;
    }
}

// Pruebas - fácil de burlar
[TestFixture]
public class AuthenticationServiceTests
{
    private Mock<IUserRepository> _mockRepository;
    private Mock<IPasswordHasher> _mockHasher;
    private AuthenticationService _service;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockHasher = new Mock<IPasswordHasher>();
        _service = new AuthenticationService(_mockRepository.Object, _mockHasher.Object);
    }

    [Test]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsUser()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com" };
        _mockRepository.Setup(r => r.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        _mockHasher.Setup(h => h.VerifyPassword("password", user.PasswordHash))
            .Returns(true);

        // Act
        var result = await _service.AuthenticateAsync("test@example.com", "password");

        // Assert
        Assert.AreEqual(user.Id, result.Id);
        _mockRepository.Verify(r => r.GetByEmailAsync("test@example.com"), Times.Once);
    }
}
```

---

## 7. Objetos de Transferencia de Datos (DTOs)

### Decisión: Usar DTOs Explícitos para Contratos de API

### ¿Por qué DTOs?

**Sin DTOs (Devolver Entidad Directamente):**
```csharp
// ❌ ANTIPATRÓN
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(int id)
{
    var user = await _userRepository.GetByIdAsync(id);
    return Ok(user); // Devuelve entidad User con TODAS las propiedades
}

// Respuesta:
// {
//   "id": 1,
//   "email": "user@example.com",
//   "passwordHash": "$2b$12$...", // ¡EXPUESTO! Nunca debería ser público
//   "phoneNumber": "+1234567890",
//   "address": "123 Main St",
//   "internalNotes": "Cliente marcado por actividad sospechosa",
//   "isDeleted": false,
//   "deletedAt": null,
//   "createdAt": "2024-01-01",
//   "lastLoginAt": "2024-03-05"
// }

// Problemas:
// 1. Seguridad: Exponiendo hash de contraseña, notas internas, banderas de eliminación
// 2. Acoplamiento: El cliente depende de la estructura exacta de la entidad
// 3. Versionado: No se puede cambiar la entidad sin romper la API
// 4. Datos innecesarios: Devolviendo campos que los clientes no necesitan
// 5. Rendimiento: Serializando datos que no se usarán
```

**Con DTOs (Contratos Explícitos):**
```csharp
// Capa de Aplicación - DTO explícito
public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    // ¡Solo propiedades públicas!
    // Sin passwordHash, internalNotes, etc.
}

[HttpGet("{id}")]
public async Task<IActionResult> GetUser(int id)
{
    var user = await _userRepository.GetByIdAsync(id);
    var dto = _mapper.Map<UserDto>(user);
    return Ok(dto);
}

// Respuesta:
// {
//   "id": 1,
//   "email": "user@example.com",
//   "phoneNumber": "+1234567890"
// }

// Beneficios:
// 1. Seguridad: Solo exponer propiedades previstas
// 2. Desacoplamiento: Contrato de API independiente de la estructura de entidad
// 3. Versionado: Se pueden tener UserDtoV1, UserDtoV2, etc.
// 4. Rendimiento: Solo serializar lo que se necesita
// 5. Validación: Se pueden incluir reglas de validación por DTO
```

### Jerarquía de DTOs en Atlantic Orders:

```
Capa de Dominio (Entidad)
│
├─ Entidad User
│  └─ Todas las propiedades (id, email, passwordHash, refreshTokens, etc.)
│
Capa de Aplicación (DTOs)
├─ LoginRequest DTO
│  └─ email, password
├─ AuthResponseDto
│  └─ accessToken, refreshToken, user (UserBasicDto)
├─ UserBasicDto
│  └─ id, email, phoneNumber
└─ UserDetailDto
   └─ id, email, phoneNumber, createdAt, lastLoginAt

Capa de Respuesta de API
└─ El cliente solo ve DTOs, nunca entidades
```

### Implementación con AutoMapper:

```csharp
// Configuración de mapeo
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Entidad -> DTO (operaciones de lectura)
        CreateMap<User, UserBasicDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber));

        // DTO -> Entidad (operaciones de escritura)
        CreateMap<CreateUserRequest, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()); // Establecer por separado
    }
}

// Uso en controlador
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(int id)
{
    var user = await _userRepository.GetByIdAsync(id);
    if (user == null)
        return NotFound();

    var dto = _mapper.Map<UserBasicDto>(user);
    return Ok(dto);
}

// Uso en servicio
public async Task<AuthResponseDto> LoginAsync(LoginRequest request)
{
    var user = await _authService.AuthenticateAsync(request.Email, request.Password);
    if (user == null)
        return null;

    var userDto = _mapper.Map<UserBasicDto>(user);
    var accessToken = _jwtTokenProvider.GenerateAccessToken(user);
    var refreshToken = _authService.GenerateRefreshToken(user.Id);

    return new AuthResponseDto
    {
        User = userDto,
        AccessToken = accessToken,
        RefreshToken = refreshToken.Token
    };
}
```

### Validación de DTOs:

```csharp
public class LoginRequest
{
    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido")]
    public string Email { get; set; }

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [StringLength(128, MinimumLength = 8, 
        ErrorMessage = "La contraseña debe tener entre 8 y 128 caracteres")]
    public string Password { get; set; }
}

// La validación sucede automáticamente en el controlador
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState); // 400 con errores de validación

    var result = await _authService.AuthenticateAsync(request.Email, request.Password);
    if (result == null)
        return Unauthorized(); // 401

    return Ok(result);
}
```

### Estrategia de Versionado de DTOs:

```csharp
// API v1
[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDtoV1>> GetUser(int id)
    {
        // Devuelve información básica del usuario
    }
}

// API v2 (se agregaron más campos)
[ApiController]
[Route("api/v2/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDtoV2>> GetUser(int id)
    {
        // Devuelve información del usuario + campos nuevos
    }
}

// Los clientes antiguos siguen funcionando con V1, los nuevos obtienen V2
```

---

## 8. Entity Framework Core

### Decisión: Usar Entity Framework Core como ORM

### ¿Por qué Entity Framework Core?

**Alternativas Consideradas:**

```
┌────────────────────────────────────────────────────────┐
│ Tecnología  │ Pros              │ Contras            │
├────────────────────────────────────────────────────────┤
│ EF Core     │ • Soporte LINQ    │ • Más memoria      │
│             │ • Type-safe       │ • Rendimiento lento│
│             │ • Migraciones auto│ • Bugs lazy loading│
├────────────────────────────────────────────────────────┤
│ Dapper      │ • Rápido          │ • Sin LINQ         │
│             │ • Ligero          │ • Mapeo manual     │
│             │ • Control         │ • Boilerplate      │
├────────────────────────────────────────────────────────┤
│ NHibernate  │ • Maduro          │ • Aprendizaje      │
│             │ • Poderoso        │ • Config compleja  │
│             │ • Cache           │ • Sobrecarga       │
├────────────────────────────────────────────────────────┤
│ ADO.NET     │ • Control total   │ • Mucho código     │
│             │ • Rápido          │ • Inyección SQL    │
│             │ • Ligero          │ • Mapeo manual     │
└────────────────────────────────────────────────────────┘
```

### Por Qué Elegimos EF Core:

1. **Soporte LINQ** - Consultas type-safe sin escribir SQL
   ```csharp
   // Con EF Core - Type safe, soporte Intellisense
   var users = await _context.Users
       .Where(u => u.Email == email && u.IsActive)
       .OrderBy(u => u.CreatedAt)
       .ToListAsync();

   // Con Dapper - SQL basado en string
   var users = await _connection.QueryAsync<User>(
       "SELECT * FROM Users WHERE Email = @Email AND IsActive = 1 ORDER BY CreatedAt",
       new { Email = email }
   );
   ```

2. **Migraciones Automáticas** - Versionado de esquema sin scripts manuales
   ```csharp
   // Agregar migración
   dotnet ef migrations add AddUserPhoneNumber
   
   // Ver código de migración (generado automáticamente)
   public partial class AddUserPhoneNumber : Migration
   {
       protected override void Up(MigrationBuilder migrationBuilder)
       {
           migrationBuilder.AddColumn<string>(
               name: "PhoneNumber",
               table: "Users",
               type: "nvarchar(20)",
               nullable: true);
       }
   }
   
   // Aplicar
   dotnet ef database update
   ```

3. **Seguimiento de Cambios** - Verificación automática de cambios
   ```csharp
   var user = await _context.Users.FindAsync(1);
   user.Email = "new@example.com";
   // EF Core detecta el cambio automáticamente
   await _context.SaveChangesAsync(); // Genera UPDATE automáticamente
   ```

4. **Relaciones** - Carga automática de claves externas
   ```csharp
   var user = await _context.Users
       .Include(u => u.RefreshTokens)
       .FirstOrDefaultAsync(u => u.Id == 1);
   
   // user.RefreshTokens se carga automáticamente
   foreach (var token in user.RefreshTokens)
   {
       Console.WriteLine(token.Token);
   }
   ```

### Compensaciones - ¿Por Qué No Dapper para Mejor Rendimiento?

```csharp
// Enfoque EF Core (Más fácil, ligeramente más lento)
[HttpGet("active-users")]
public async Task<IActionResult> GetActiveUsers()
{
    var users = await _context.Users
        .Where(u => u.IsActive && u.CreatedAt > DateTime.Now.AddMonths(-1))
        .Select(u => new UserDto 
        { 
            Id = u.Id, 
            Email = u.Email 
        })
        .ToListAsync();

    return Ok(users);
}
// SQL Generado: Óptimo, parametrizado automáticamente
// Rendimiento: ~50-100ms para 10,000 usuarios
// Legibilidad del código: Alta

// Enfoque Dapper (Más rápido, más código)
[HttpGet("active-users")]
public async Task<IActionResult> GetActiveUsers()
{
    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    var sql = @"
        SELECT Id, Email FROM Users 
        WHERE IsActive = 1 AND CreatedAt > @CreatedAfter";

    var users = (await connection.QueryAsync<UserDto>(sql, 
        new { CreatedAfter = DateTime.Now.AddMonths(-1) }))
        .ToList();

    return Ok(users);
}
// SQL Generado: Optimizado manualmente
// Rendimiento: ~20-30ms para 10,000 usuarios (70% más rápido)
// Legibilidad del código: Media

// Compensación: EF Core agrega ~30ms de latencia pero:
// ✅ El código es más mantenible
// ✅ Las consultas type-safe reducen bugs
// ✅ Las relaciones se manejan automáticamente
// ✅ Las migraciones se administran automáticamente
//
// Para Atlantic Orders (aplicación empresarial, no sistema en tiempo real),
// La diferencia de 30ms es negligible, la mantenibilidad es crítica
```

### Uso de EF Core en Atlantic Orders:

```csharp
// Configuración de DbContext
public class AtlanticOrdersDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Usuario -> RefreshTokens (uno-a-muchos)
        modelBuilder.Entity<User>()
            .HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Cascada de eliminación

        // Orden -> OrderItems (uno-a-muchos)
        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Orden -> Usuario (muchos-a-uno)
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId);
    }
}

// El Repositorio usa DbContext
public class UserRepository : IUserRepository
{
    private readonly AtlanticOrdersDbContext _context;

    public async Task<User> GetByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking() // Rendimiento: No rastrear para consultas de solo lectura
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User> GetByIdWithTokensAsync(int id)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens) // Carga entusiasta de datos relacionados
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }
}
```

---

## 9. Inyección de Dependencias

### Decisión: Usar Contenedor de Inyección de Dependencias Integrado en ASP.NET Core

### ¿Por qué Inyección de Dependencias?

**Sin DI (Acoplamiento Fuerte):**
```csharp
// ❌ ANTIPATRÓN: Dependencias hard-coded
public class AuthController : ControllerBase
{
    private readonly AuthenticationService _authService;
    private readonly UserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenProvider _jwtTokenProvider;

    public AuthController()
    {
        // Crear dependencias internamente - ACOPLAMIENTO FUERTE
        var dbContext = new AtlanticOrdersDbContext();
        _userRepository = new UserRepository(dbContext);
        _passwordHasher = new PasswordHasher();
        _jwtTokenProvider = new JwtTokenProvider(new ConfigurationBuilder().Build());
        _authService = new AuthenticationService(_userRepository, _passwordHasher);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Difícil de probar - no se puede inyectar AuthenticationService simulado
        return Ok(await _authService.AuthenticateAsync(request.Email, request.Password));
    }
}

// Problemas:
// 1. Difícil de probar - el constructor crea dependencias reales
// 2. Difícil de cambiar implementaciones (crear subclase nueva? ¿Usar reflexión?)
// 3. Gestión del alcance - La vida útil de DbContext no se administra
// 4. Configuración - Las credenciales están hard-coded o dependen del entorno
// 5. Dependencias anidadas profundamente - difícil de entender los requisitos
```

**Con Inyección de Dependencias (Acoplamiento Flojo):**
```csharp
// ✅ BUEN PATRÓN: Inyectar dependencias
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    // Las dependencias se inyectan por el contenedor DI
    public AuthController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        return Ok(await _authService.AuthenticateAsync(request.Email, request.Password));
    }
}

// Beneficios:
// 1. Fácil de probar - inyectar IAuthenticationService simulado
// 2. Fácil de cambiar implementaciones - clase diferente, misma interfaz
// 3. Gestión del alcance - El contenedor DI maneja las vidas útiles
// 4. Configuración - Centralizada en Startup
// 5. Dependencias claras - El constructor muestra los requisitos
```

### Configuración de DI en Atlantic Orders:

```csharp
// Program.cs - Configuración DI central
var builder = WebApplication.CreateBuilder(args);

// Registrar servicios en el contenedor DI
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();
builder.Services.AddScoped<IRateLimitService, RateLimitService>();

// Registrar DbContext con cadena de conexión
builder.Services.AddDbContext<AtlanticOrdersDbContext>(options =>
    options.UseInMemoryDatabase("AtlanticOrders") // o UseSqlServer, etc.
);

// Registrar AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Registrar configuración
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

var app = builder.Build();

// El contenedor DI ahora tiene todos los servicios registrados
// Cuando el controlador solicita IAuthenticationService, el contenedor proporciona:
// - IUserRepository -> UserRepository (desde DI)
// - IPasswordHasher -> PasswordHasher (desde DI)
// - AuthenticationService con dependencias inyectadas
```

### Vidas Útiles de Servicio:

```csharp
// Transient - Nueva instancia cada vez
// Usar para: Utilidades sin estado (PasswordHasher, DateTimeProvider)
builder.Services.AddTransient<IPasswordHasher, PasswordHasher>();
// Ejemplo:
// Solicitud 1: Instancia PasswordHasher A
// Solicitud 2: Instancia PasswordHasher B
// Solicitud 3: Instancia PasswordHasher A (nueva)

// Scoped - Una instancia por solicitud HTTP
// Usar para: DbContext, Repositorios, Servicios
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<AtlanticOrdersDbContext>();
// Ejemplo:
// Solicitud 1: Instancia UserRepository A
//   - Middleware llama userRepo
//   - Servicio llama userRepo
//   - ¡Misma instancia! (cambios rastreados juntos)
// Solicitud 2: Instancia UserRepository B (fresca)

// Singleton - Una instancia para toda la aplicación
// Usar para: Configuración, Registro, Cachés
builder.Services.AddSingleton<IApplicationConfiguration>(
    new ApplicationConfiguration(builder.Configuration));
builder.Services.AddSingleton<IConnectionPoolCache>(
    new ConnectionPoolCache(50)); // Compartido en todas las solicitudes
// Ejemplo:
// Solicitud 1: Instancia Configuration A
// Solicitud 2: Instancia Configuration A (misma)
// Vida de aplicación: Instancia Configuration A
```

### Por Qué DbContext Debe Ser Scoped:

```csharp
// ❌ INCORRECTO: DbContext Singleton
builder.Services.AddSingleton<AtlanticOrdersDbContext>();

[HttpPost("create-users")]
public async Task<IActionResult> CreateUsers()
{
    // Thread 1: La solicitud crea Usuario A, agrega a DbContext
    var user1 = new User { Email = "user1@example.com" };
    await _context.Users.AddAsync(user1);
    
    // Thread 2: Otra solicitud también usa el MISMO DbContext
    var user2 = new User { Email = "user2@example.com" };
    await _context.Users.AddAsync(user2);
    
    // Thread 1: Guarda cambios
    await _context.SaveChangesAsync();
    // Resultado: ¡Se guardaron ambos usuarios! (no era intención)
    
    // DbContext rastrea ambos usuarios entre solicitudes
    // ¡Fugas de memoria, inconsistencia de datos!
}

// ✅ CORRECTO: DbContext Scoped
builder.Services.AddScoped<AtlanticOrdersDbContext>();

[HttpPost("create-users")]
public async Task<IActionResult> CreateUsers()
{
    // Thread 1: La solicitud obtiene instancia DbContext A
    var user1 = new User { Email = "user1@example.com" };
    await _context.Users.AddAsync(user1); // Instancia A rastrea cambios
    await _context.SaveChangesAsync(); // Instancia A guarda, luego se desecha
    
    // Thread 2: Otra solicitud obtiene instancia DbContext B
    var user2 = new User { Email = "user2@example.com" };
    await _context.Users.AddAsync(user2); // Instancia B rastrea cambios
    await _context.SaveChangesAsync(); // Instancia B guarda, luego se desecha
    
    // Separación limpia - ¡sin contaminación cruzada de solicitudes!
}
```

### Pruebas con Inyección de Dependencias:

```csharp
[TestFixture]
public class AuthenticationServiceTests
{
    private Mock<IUserRepository> _mockUserRepository;
    private Mock<IPasswordHasher> _mockPasswordHasher;
    private AuthenticationService _service;

    [SetUp]
    public void Setup()
    {
        // Inyectar dependencias simuladas
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _service = new AuthenticationService(_mockUserRepository.Object, _mockPasswordHasher.Object);
    }

    [Test]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsUser()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com" };
        _mockUserRepository
            .Setup(r => r.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        _mockPasswordHasher
            .Setup(h => h.VerifyPassword("password", user.PasswordHash))
            .Returns(true);

        // Act
        var result = await _service.AuthenticateAsync("test@example.com", "password");

        // Assert
        Assert.AreEqual(user.Id, result.Id);
    }
}
```

---

## 10. Manejo de Errores y Registro

### Decisión: Manejo Centralizado de Excepciones con Middleware + Registro Estructurado

### ¿Por qué Manejo de Excepciones Basado en Middleware?

**Sin Middleware (Try-Catch en Cada Controlador):**
```csharp
// ❌ ANTIPATRÓN: Manejo de excepciones disperso
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    try
    {
        var user = await _authService.AuthenticateAsync(request.Email, request.Password);
        if (user == null)
            return Unauthorized("Credenciales inválidas");

        return Ok(user);
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "Error de BD durante login");
        return StatusCode(500, "Error de BD");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error inesperado durante login");
        return StatusCode(500, "Error interno del servidor");
    }
}

[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
{
    try
    {
        var user = await _userService.CreateUserAsync(request);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "Error de BD durante registro");
        return StatusCode(500, "Error de BD");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error inesperado durante registro");
        return StatusCode(500, "Error interno del servidor");
    }
}

// Problemas:
// 1. Duplicación de código - mismo manejo de errores en cada método
// 2. Respuestas inconsistentes - diferentes controladores podrían manejar el mismo error diferente
// 3. Difícil de mantener - agregar nuevo tipo de error requiere actualizar todos los controladores
// 4. Preocupaciones mixtas - lógica empresarial mezclada con manejo de errores
// 5. Registro inconsistente - diferentes formatos, contexto faltante
```

**Con Middleware (Manejo Centralizado de Excepciones):**
```csharp
// Middleware global de manejo de excepciones
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case ValidationException vex:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.StatusCode = 400;
                response.Message = vex.Message;
                response.Errors = vex.Errors;
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                response.StatusCode = 401;
                response.Message = "Acceso no autorizado";
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                response.StatusCode = 404;
                response.Message = "Recurso no encontrado";
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.StatusCode = 500;
                response.Message = "Error interno del servidor";
                response.TraceId = context.TraceIdentifier;
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}

// Registrar en Program.cs
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Ahora los controladores están limpios
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var user = await _authService.AuthenticateAsync(request.Email, request.Password);
    if (user == null)
        throw new UnauthorizedAccessException("Credenciales inválidas");

    return Ok(user);
}

// Beneficios:
// 1. Sin duplicación - manejo de errores centralizado
// 2. Respuestas consistentes - todos los errores formateados de la misma manera
// 3. Fácil de mantener - agregar nuevo tipo de error una vez en middleware
// 4. Separación de preocupaciones - los controladores se enfocan en lógica empresarial
// 5. Registro consistente - todas las excepciones registradas uniformemente
```

### Registro Estructurado:

```csharp
// Usando Serilog para registro estructurado
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Intento de inicio de sesión para usuario {Email}", request.Email);

        var user = await _authService.AuthenticateAsync(request.Email, request.Password);
        if (user == null)
        {
            _logger.LogWarning("Intento de inicio de sesión fallido para usuario {Email}", request.Email);
            throw new UnauthorizedAccessException("Credenciales inválidas");
        }

        _logger.LogInformation("Inicio de sesión exitoso para usuario {UserId} ({Email})", user.Id, user.Email);
        return Ok(user);
    }
}

// Salida de registro estructurado (JSON):
// {
//   "timestamp": "2024-03-05T10:30:45.123Z",
//   "level": "Information",
//   "message": "Login attempt for user {Email}",
//   "email": "user@example.com",
//   "source": "AuthController",
//   "requestId": "0HN3DGFR82KAV:00000001"
// }

// Ventajas:
// 1. Legible por máquina - fácil de analizar y agregar
// 2. Buscable - se pueden consultar registros por Email, UserId, etc.
// 3. Rastreable - RequestId correlaciona entradas de registro relacionadas
// 4. Contextual - incluye datos empresariales relevantes
```

### Excepciones Personalizadas:

```csharp
// Excepción personalizada para errores de validación
public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; set; }

    public ValidationException(Dictionary<string, string[]> errors)
        : base("Validación fallida")
    {
        Errors = errors;
    }
}

// Usada en capa de servicio
public async Task<User> CreateUserAsync(CreateUserRequest request)
{
    var errors = new Dictionary<string, string[]>();

    // Validar formato de correo electrónico
    if (!IsValidEmail(request.Email))
        errors["email"] = new[] { "Formato de correo electrónico inválido" };

    // Validar fortaleza de contraseña
    if (request.Password.Length < 8)
        errors["password"] = new[] { "La contraseña debe tener al menos 8 caracteres" };

    if (errors.Count > 0)
        throw new ValidationException(errors);

    // Continuar con la creación de usuario...
}

// El middleware captura y formatea
// Respuesta: 400 Bad Request
// {
//   "statusCode": 400,
//   "message": "Validación fallida",
//   "errors": {
//     "email": ["Formato de correo electrónico inválido"],
//     "password": ["La contraseña debe tener al menos 8 caracteres"]
//   }
// }
```

---

## 11. CORS y Encabezados de Seguridad

### Decisión: CORS Estricto + Encabezados de Seguridad

### ¿Por qué CORS Estricto?

**Riesgo de Seguridad CORS:**
```
Escenario: Sitio web del atacante (attacker.com) hace solicitud a API
El navegador incluye automáticamente cookies/encabezados de auth en solicitud:

[Sitio Web del Atacante]
  └─ JavaScript: fetch('https://atlantic-orders.com/api/orders')
     └─ El navegador envía encabezado Authorization automáticamente
     └─ Sin CORS, ¡el atacante puede leer la respuesta!

CORS Abierto:
  "Access-Control-Allow-Origin": "*"
  └─ Cualquier sitio web puede llamar tu API
  └─ Sin protección contra exploración CSRF, XSS

CORS Estricto:
  "Access-Control-Allow-Origin": "https://atlantic-orders-web.com"
  └─ Solo el dominio frontend confiable permitido
  └─ El navegador bloquea solicitudes de otros orígenes
```

### Implementación:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Definir orígenes permitidos (la producción debería ser desde config)
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? 
    new[] { "http://localhost:3000", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("StrictPolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)  // Solo estos dominios
            .AllowAnyMethod()              // GET, POST, PUT, DELETE, etc.
            .AllowAnyHeader()              // Todos los encabezados
            .AllowCredentials()            // Permitir cookies/encabezados de auth
            .WithExposedHeaders("X-Total-Count", "X-Total-Pages"); // Encabezados personalizados que el cliente necesita
    });
});

var app = builder.Build();

// IMPORTANTE: CORS debe venir antes del middleware de auth
app.UseCors("StrictPolicy");
app.UseAuthentication();
app.UseAuthorization();

// appsettings.json
{
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:5173",
    "https://atlantic-orders.example.com"
  ]
}
```

### Encabezados de Seguridad:

```csharp
// Middleware para encabezados de seguridad
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Content Security Policy - previene ataques XSS e inyección
        context.Response.Headers.Add(
            "Content-Security-Policy",
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'"
        );

        // X-Content-Type-Options - previene ejecución de MIME type
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

        // X-Frame-Options - previene clickjacking
        context.Response.Headers.Add("X-Frame-Options", "DENY");

        // X-XSS-Protection - habilita protección XSS del navegador
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

        // Strict-Transport-Security - obliga HTTPS
        context.Response.Headers.Add(
            "Strict-Transport-Security",
            "max-age=31536000; includeSubDomains"
        );

        // Referrer-Policy - controla información del referrer
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

        await _next(context);
    }
}

// Registrar en Program.cs
app.UseMiddleware<SecurityHeadersMiddleware>();
```

**Lo Que Hace Cada Encabezado:**

```
Encabezado                          Previene
─────────────────────────────────────────────────────────
Content-Security-Policy             XSS, ataques de inyección
X-Content-Type-Options              Ejecución de MIME (JavaScript como CSS)
X-Frame-Options                     Clickjacking (incrustar página en iframe)
X-XSS-Protection                    Bypass de filtro XSS del navegador
Strict-Transport-Security           Man-in-the-middle (obliga HTTPS)
Referrer-Policy                     Fuga de información vía encabezado referrer
```

---

## 12. Estrategia de Pruebas

### Decisión: Pruebas Unitarias + Pruebas de Integración + Enfoque Piramidal

**Pirámide de Pruebas:**
```
              /\
             /  \
            /    \  Pruebas E2E (10%)
           /      \  - Pruebas de flujo completo
          /────────\
         /          \
        /            \  Pruebas de Integración (20%)
       /              \  - BD, servicios externos
      /────────────────\
     /                  \
    /                    \  Pruebas Unitarias (70%)
   /                      \  - Métodos individuales
  /──────────────────────────\
```

### Pruebas Unitarias:

```csharp
[TestFixture]
public class AuthenticationServiceTests
{
    private Mock<IUserRepository> _mockUserRepository;
    private Mock<IPasswordHasher> _mockPasswordHasher;
    private AuthenticationService _service;

    [SetUp]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _service = new AuthenticationService(_mockUserRepository.Object, _mockPasswordHasher.Object);
    }

    [Test]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsUser()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com" };
        _mockUserRepository
            .Setup(r => r.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        _mockPasswordHasher
            .Setup(h => h.VerifyPassword("password", user.PasswordHash))
            .Returns(true);

        // Act
        var result = await _service.AuthenticateAsync("test@example.com", "password");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
    }

    [Test]
    public async Task AuthenticateAsync_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com" };
        _mockUserRepository
            .Setup(r => r.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        _mockPasswordHasher
            .Setup(h => h.VerifyPassword("wrongpassword", user.PasswordHash))
            .Returns(false);

        // Act
        var result = await _service.AuthenticateAsync("test@example.com", "wrongpassword");

        // Assert
        Assert.IsNull(result);
    }

    [Test]
    public async Task AuthenticateAsync_WithNonexistentEmail_ReturnsNull()
    {
        // Arrange
        _mockUserRepository
            .Setup(r => r.GetByEmailAsync("notfound@example.com"))
            .ReturnsAsync((User)null);

        // Act
        var result = await _service.AuthenticateAsync("notfound@example.com", "password");

        // Assert
        Assert.IsNull(result);
        _mockPasswordHasher.Verify(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), 
            Times.Never); // La verificación de contraseña no debería ocurrir
    }
}
```

### Pruebas de Integración:

```csharp
[TestFixture]
public class AuthControllerIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private AtlanticOrdersDbContext _dbContext;

    [SetUp]
    public async Task Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Usar BD en memoria para pruebas
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<AtlanticOrdersDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<AtlanticOrdersDbContext>(options =>
                        options.UseInMemoryDatabase("TestDb"));
                });
            });

        _client = _factory.CreateClient();
        _dbContext = _factory.Services.GetService<AtlanticOrdersDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
    }

    [Test]
    public async Task Login_WithValidCredentials_Returns200AndToken()
    {
        // Arrange - Sembrar usuario de prueba
        var passwordHasher = new PasswordHasher();
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = passwordHasher.HashPassword("Test@123456")
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.PostAsync("/auth/login",
            new StringContent(
                JsonSerializer.Serialize(new { email = "test@example.com", password = "Test@123456" }),
                Encoding.UTF8,
                "application/json"));

        // Assert
        Assert.AreEqual(StatusCodes.Status200OK, (int)response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.IsNotNull(result.GetProperty("accessToken"));
    }

    [Test]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        // Arrange
        var passwordHasher = new PasswordHasher();
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = passwordHasher.HashPassword("Test@123456")
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.PostAsync("/auth/login",
            new StringContent(
                JsonSerializer.Serialize(new { email = "test@example.com", password = "WrongPassword" }),
                Encoding.UTF8,
                "application/json"));

        // Assert
        Assert.AreEqual(StatusCodes.Status401Unauthorized, (int)response.StatusCode);
    }

    [TearDown]
    public async Task Teardown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
```

### ¿Por Qué Este Enfoque de Pruebas?

```
✅ Pruebas Unitarias (70%):
  - Rápidas de ejecutar (~1ms cada una)
  - Prueban lógica empresarial de forma aislada
  - Burlar dependencias externas
  - Alta cobertura de casos extremos

✅ Pruebas de Integración (20%):
  - Prueban interacciones reales con BD
  - Verifican que los endpoints API funcionen de extremo a extremo
  - Usar BD en memoria para velocidad
  - Más lentas pero más realistas

✅ Pruebas E2E (10%):
  - Probar contra API en vivo
  - Automatización del navegador (Selenium, Playwright)
  - Flujos de usuario reales
  - Lentas, costosas, pero solo rutas críticas
```

---

## Matriz Resumen

| Decisión | Elección | Por Qué | Compensación |
|----------|--------|-----|-----------|
| **Arquitectura** | Limpia 4-Capas | Independiente, comprobable, escalable | Más archivos, complejidad inicial |
| **Auth** | JWT + Actualización | Sin estado, escalable, móvil-friendly | No se puede revocar al instante, tokens más grandes |
| **Limitación de Velocidad** | Por IP por endpoint | Previene fuerza bruta, simple | Los usuarios de VPN podrían bloquearse |
| **Hash de Contraseña** | BCrypt 12 rondas | Adaptativo, lento (seguridad), salted | Toma 100-200ms por inicio de sesión |
| **Rotación de Token** | Nuevo actualizar en cada uso | Detecta robo, limita daño | Más escrituras en BD |
| **Repositorios** | Patrón de Repositorio | Comprobable, desacoplado de EF | Más sobrecarga de abstracción |
| **DTOs** | Explícito por endpoint | Seguridad, versionado, contratos | Más código de mapeo |
| **ORM** | Entity Framework Core | LINQ, migraciones, relaciones | ~30ms de latencia vs SQL manual |
| **DI** | Contenedor integrado | Simple, integrado, comprobable | Menos potente que terceros |
| **Errores** | Middleware + excepciones personalizadas | Centralizado, consistente, reutilizable | No se puede ser selectivo en capturas |
| **CORS** | Lista blanca estricta | Seguridad, previene mal uso | Requiere configuración del frontend |
| **Pruebas** | Enfoque piramidal | Equilibrado, retroalimentación rápida, realista | Requiere disciplina |

---

## Cuándo Revisar Estas Decisiones

- **Si el rendimiento se vuelve crítico**: Considerar Dapper para consultas intensivas en lectura
- **Si el sistema escala a miles de usuarios**: Implementar CQRS para separación lectura/escritura
- **Si se necesitan microservicios**: Cambiar a gRPC o arquitectura basada en eventos
- **Si se requiere tiempo real**: Agregar soporte WebSocket, considerar sesiones distribuidas
- **Si el equipo crece**: Agregar capas de abstracción adicionales para diseño orientado al dominio
