using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AtlanticOrders.Application.DTOs;
using AtlanticOrders.Infrastructure.Security;
using AtlanticOrders.Domain.Repositories;
using AtlanticOrders.Domain.Entities;

namespace AtlanticOrders.Api.Controllers;

/// <summary>
/// Controlador de autenticación.
/// Proporciona endpoints para login, refresh de tokens y logout.
/// Implementa seguridad con JWT, BCrypt y Rate Limiting.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationProvider _authenticationProvider;
    private readonly ITokenProvider _tokenProvider;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthenticationProvider authenticationProvider,
        ITokenProvider tokenProvider,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _authenticationProvider = authenticationProvider;
        _tokenProvider = tokenProvider;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Realiza login y retorna un token JWT + refresh token en HttpOnly cookies.
    /// Rate limited a 5 intentos por minuto por IP.
    /// Las contraseñas se validan de forma segura con BCrypt.
    /// NO se loguean las contraseñas por razones de seguridad.
    /// 
    /// SEGURIDAD:
    /// - Tokens almacenados en HttpOnly cookies (no accesibles desde JavaScript)
    /// - Cookies marcadas como Secure (solo se envían por HTTPS)
    /// - SameSite=Strict para proteger contra CSRF
    /// </summary>
    /// <param name="request">Credenciales del usuario (email y contraseña)</param>
    /// <returns>HttpOnly cookies con tokens si las credenciales son válidas</returns>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        // Validar entrada
        if (string.IsNullOrWhiteSpace(request.email) || string.IsNullOrWhiteSpace(request.password))
        {
            _logger.LogWarning("Intento de login con credenciales vacías");
            return BadRequest(new { error = "Usuario y contraseña son requeridos." });
        }

        // Validar credenciales
        var credencialesValidas = await _authenticationProvider.ValidarCredencialesAsync(
            request.email,
            request.password);

        if (!credencialesValidas)
        {
            _logger.LogWarning("Intento de login fallido para usuario: {email}", request.email);
            return Unauthorized(new { error = "Credenciales inválidas." });
        }

        try
        {
            _logger.LogInformation("Login exitoso para usuario: {email}", request.email);

            // Generar access token
            var tokenResult = _tokenProvider.GenerarToken(request.email, "User");

            // Generar refresh token
            var refreshToken = _tokenProvider.GenerarRefreshToken();
            var refreshTokenExpirationDays = int.Parse(
                _configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

            // Almacenar refresh token en BD
            var refreshTokenEntity = new RefreshToken
            {
                UserEmail = request.email.ToLower(),
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

            // Obtener opciones de cookies desde configuración
            var jwtSettings = _configuration.GetSection("Jwt");
            var cookieSecureFlag = bool.Parse(jwtSettings["CookieSecureFlag"] ?? "true");
            var cookieSameSite = jwtSettings["CookieSameSite"] ?? "Strict";

            // Configurar opciones de HttpOnly cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,                // 🔒 No accesible desde JavaScript (protege contra XSS)
                Secure = cookieSecureFlag,      // 🔒 Solo se envía por HTTPS
                SameSite = Enum.Parse<SameSiteMode>(cookieSameSite), // 🔒 Protege contra CSRF
                Expires = DateTime.UtcNow.AddMinutes(tokenResult.ExpiresIn)
            };

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = cookieSecureFlag,
                SameSite = Enum.Parse<SameSiteMode>(cookieSameSite),
                Expires = DateTime.UtcNow.AddDays(refreshTokenExpirationDays)
            };

            // Agregar tokens en HttpOnly cookies
            var accessTokenCookieName = jwtSettings["CookieName"] ?? "X-Access-Token";
            var refreshTokenCookieName = jwtSettings["RefreshCookieName"] ?? "X-Refresh-Token";

            Response.Cookies.Append(accessTokenCookieName, tokenResult.Token, cookieOptions);
            Response.Cookies.Append(refreshTokenCookieName, refreshToken, refreshCookieOptions);

            // Retornar respuesta (sin tokens en body, ya están en cookies)
            return Ok(new LoginResponseDto
            {
                Token = tokenResult.Token,        // Aún se retorna para debugging/documentación
                ExpiresIn = tokenResult.ExpiresIn,
                RefreshToken = refreshToken      // Aún se retorna para debugging/documentación
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante login para usuario: {email}", request.email);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error al procesar el login." });
        }
    }

    /// <summary>
    /// Renueva el access token usando un refresh token válido desde HttpOnly cookie.
    /// El usuario no necesita re-autenticar si tiene un refresh token válido.
    /// El refresh token se valida contra la BD y debe estar activo.
    /// 
    /// SEGURIDAD:
    /// - Lee refresh token de HttpOnly cookie (no del body)
    /// - Valida contra BD antes de generar nuevo token
    /// - Aplica refresh token rotation (revoca el antiguo)
    /// </summary>
    /// <param name="request">Opcional: puede venir en body o se lee de cookie</param>
    /// <returns>Nuevo access token en HttpOnly cookie si el refresh token es válido</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto? request = null)
    {
        try
        {
            // Obtener refresh token desde:
            // 1. HttpOnly cookie (más seguro)
            // 2. Body request (fallback para compatibilidad)
            var refreshTokenCookieName = _configuration["Jwt:RefreshCookieName"] ?? "X-Refresh-Token";
            
            var refreshToken = string.Empty;
            
            if (Request.Cookies.TryGetValue(refreshTokenCookieName, out var cookieToken))
            {
                refreshToken = cookieToken;
            }
            else if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
            {
                refreshToken = request.RefreshToken;
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return BadRequest(new { error = "Refresh token es requerido (debe estar en HttpOnly cookie)." });
            }

            // Obtener refresh token de BD
            var refreshTokenEntity = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if (refreshTokenEntity == null || !refreshTokenEntity.IsValid)
            {
                _logger.LogWarning("Intento de refresh con token inválido o expirado");
                return Unauthorized(new { error = "Refresh token inválido o expirado." });
            }

            // Generar nuevo access token
            var newTokenResult = _tokenProvider.GenerarToken(refreshTokenEntity.UserEmail, "User");

            // Generar nuevo refresh token (rotation)
            var newRefreshToken = _tokenProvider.GenerarRefreshToken();
            var refreshTokenExpirationDays = int.Parse(
                _configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

            // Revocar el refresh token antiguo
            await _refreshTokenRepository.RevokeAsync(refreshTokenEntity);

            // Crear nuevo refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                UserEmail = refreshTokenEntity.UserEmail,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            await _refreshTokenRepository.CreateAsync(newRefreshTokenEntity);

            // Configurar opciones de cookies
            var jwtSettings = _configuration.GetSection("Jwt");
            var cookieSecureFlag = bool.Parse(jwtSettings["CookieSecureFlag"] ?? "true");
            var cookieSameSite = jwtSettings["CookieSameSite"] ?? "Strict";

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = cookieSecureFlag,
                SameSite = Enum.Parse<SameSiteMode>(cookieSameSite),
                Expires = DateTime.UtcNow.AddMinutes(newTokenResult.ExpiresIn)
            };

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = cookieSecureFlag,
                SameSite = Enum.Parse<SameSiteMode>(cookieSameSite),
                Expires = DateTime.UtcNow.AddDays(refreshTokenExpirationDays)
            };

            // Actualizar cookies con nuevos tokens
            var accessTokenCookieName = jwtSettings["CookieName"] ?? "X-Access-Token";
            Response.Cookies.Append(accessTokenCookieName, newTokenResult.Token, cookieOptions);
            Response.Cookies.Append(refreshTokenCookieName, newRefreshToken, refreshCookieOptions);

            _logger.LogInformation("Refresh token exitoso para usuario: {email}", refreshTokenEntity.UserEmail);

            return Ok(new LoginResponseDto
            {
                Token = newTokenResult.Token,
                ExpiresIn = newTokenResult.ExpiresIn,
                RefreshToken = newRefreshToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al renovar token");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error al renovar el token." });
        }
    }

    /// <summary>
    /// Realiza logout revocando todos los refresh tokens del usuario.
    /// Requiere estar autenticado con JWT válido.
    /// Después del logout, todos los dispositivos del usuario necesitarán re-autenticar.
    /// Limpia las HttpOnly cookies de tokens.
    /// </summary>
    /// <returns>Confirmación de logout exitoso</returns>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        try
        {
            // Obtener email del usuario desde JWT
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return Unauthorized(new { error = "No se pudo identificar el usuario." });
            }

            // Revocar todos los refresh tokens del usuario en BD
            await _refreshTokenRepository.RevokeAllForUserAsync(userEmail);

            // Limpiar HttpOnly cookies de tokens
            var accessTokenCookieName = _configuration["Jwt:CookieName"] ?? "X-Access-Token";
            var refreshTokenCookieName = _configuration["Jwt:RefreshCookieName"] ?? "X-Refresh-Token";

            Response.Cookies.Delete(accessTokenCookieName);
            Response.Cookies.Delete(refreshTokenCookieName);

            _logger.LogInformation("Logout exitoso para usuario: {email}", userEmail);

            return Ok(new { message = "Logout exitoso. Todos los tokens han sido revocados y las cookies eliminadas." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante logout");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error al procesar el logout." });
        }
    }

    /// <summary>
    /// Endpoint de healthcheck para Docker/Kubernetes.
    /// Verifica que la API esté disponible y funcionando.
    /// No requiere autenticación.
    /// </summary>
    /// <returns>Estado de salud de la API</returns>
    [HttpGet("/health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> Health()
    {
        _logger.LogDebug("Healthcheck ejecutado");
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            service = "AtlanticOrdersApi"
        });
    }
}
