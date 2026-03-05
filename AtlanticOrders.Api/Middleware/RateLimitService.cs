using System.Collections.Concurrent;

namespace AtlanticOrders.Api.Middleware;

/// <summary>
/// Servicio para implementar rate limiting sin dependencias externas.
/// Mantiene un registro de intentos por IP y endpoint.
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Verifica si una solicitud está permitida según las políticas de rate limiting.
    /// </summary>
    /// <param name="clientId">Identificador único del cliente (IP o User-Agent).</param>
    /// <param name="endpoint">Endpoint que se intenta acceder.</param>
    /// <returns>True si la solicitud está permitida, false si excede el límite.</returns>
    bool IsRequestAllowed(string clientId, string endpoint);

    /// <summary>
    /// Obtiene información sobre límites disponibles para un cliente.
    /// </summary>
    /// <param name="clientId">Identificador del cliente.</param>
    /// <param name="endpoint">Endpoint consultado.</param>
    /// <returns>Tupla con (límite total, intentos restantes, próximo reset en minutos).</returns>
    (int Limit, int Remaining, int ResetInSeconds) GetRateLimitInfo(string clientId, string endpoint);
}

/// <summary>
/// Implementación de Rate Limiting basada en memoria.
/// Almacena intentos en un diccionario en memoria con expiración temporal.
/// </summary>
public class InMemoryRateLimitService : IRateLimitService
{
    private readonly ConcurrentDictionary<string, RateLimitEntry> _requestCounts;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InMemoryRateLimitService> _logger;

    // Configuración de límites por endpoint
    private readonly int _generalLimit = 100; // Solicitudes por minuto (general)
    private readonly int _loginLimit = 5;      // Solicitudes por minuto (login)
    private readonly int _windowMinutes = 1;   // Ventana de tiempo

    public InMemoryRateLimitService(
        IConfiguration configuration,
        ILogger<InMemoryRateLimitService> logger)
    {
        _requestCounts = new ConcurrentDictionary<string, RateLimitEntry>();
        _configuration = configuration;
        _logger = logger;

        // Intentar leer los límites desde configuración
        var rateLimitConfig = _configuration.GetSection("RateLimiting");
        if (rateLimitConfig.Exists())
        {
            var loginRule = rateLimitConfig.GetSection("IpRateLimitPolicies:LoginRule");
            if (loginRule.Exists())
            {
                if (int.TryParse(loginRule["Limit"], out var loginLim))
                    _loginLimit = loginLim;
            }
        }
    }

    /// <summary>
    /// Verifica si un cliente puede realizar una solicitud.
    /// Cada cliente tiene su propio contador por endpoint.
    /// </summary>
    public bool IsRequestAllowed(string clientId, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(endpoint))
            return true; // Si no hay ID de cliente, permitir

        var key = $"{clientId}:{endpoint}";
        var now = DateTime.UtcNow;

        // Determinar el límite basado en el endpoint
        var limit = endpoint.Contains("login", StringComparison.OrdinalIgnoreCase) 
            ? _loginLimit 
            : _generalLimit;

        // Obtener o crear entrada de rate limit
        var entry = _requestCounts.AddOrUpdate(
            key,
            new RateLimitEntry { Count = 1, ResetTime = now.AddMinutes(_windowMinutes) },
            (k, existing) =>
            {
                // Si la ventana ha expirado, resetear
                if (now >= existing.ResetTime)
                {
                    return new RateLimitEntry { Count = 1, ResetTime = now.AddMinutes(_windowMinutes) };
                }

                // Incrementar contador
                existing.Count++;
                return existing;
            }
        );

        // Verificar si se excedió el límite
        if (entry.Count > limit)
        {
            _logger.LogWarning(
                "Rate limit exceeded for client {ClientId} on endpoint {Endpoint}. " +
                "Limit: {Limit}, Count: {Count}",
                clientId, endpoint, limit, entry.Count);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Obtiene información del rate limit para un cliente y endpoint.
    /// </summary>
    public (int Limit, int Remaining, int ResetInSeconds) GetRateLimitInfo(string clientId, string endpoint)
    {
        var key = $"{clientId}:{endpoint}";
        var limit = endpoint.Contains("login", StringComparison.OrdinalIgnoreCase) 
            ? _loginLimit 
            : _generalLimit;

        if (_requestCounts.TryGetValue(key, out var entry))
        {
            var now = DateTime.UtcNow;
            var remaining = Math.Max(0, limit - entry.Count);
            var resetInSeconds = Math.Max(0, (int)(entry.ResetTime - now).TotalSeconds);

            return (limit, remaining, resetInSeconds);
        }

        return (limit, limit, _windowMinutes * 60);
    }

    /// <summary>
    /// Limpia entradas expiradas de la memoria periodicamente.
    /// </summary>
    public void CleanupExpiredEntries()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _requestCounts
            .Where(kvp => kvp.Value.ResetTime < now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _requestCounts.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Entrada de rate limit almacenada en memoria.
    /// </summary>
    private class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTime ResetTime { get; set; }
    }
}
