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
    bool IsRequestAllowed(string clientId, string endpoint);

    /// <summary>
    /// Obtiene información sobre límites disponibles para un cliente.
    /// </summary>
    (int Limit, int Remaining, int ResetInSeconds) GetRateLimitInfo(string clientId, string endpoint);
}

/// <summary>
/// Implementación de Rate Limiting basada en memoria.
/// Lee la configuración desde appsettings.json
/// </summary>
public class InMemoryRateLimitService : IRateLimitService
{
    private readonly ConcurrentDictionary<string, RateLimitEntry> _requestCounts;
    private readonly ILogger<InMemoryRateLimitService> _logger;

    // Límites configurables desde appsettings.json
    private int _generalLimit = 100;
    private int _loginLimit = 5;
    private int _refreshLimit = 30;
    private int _windowSeconds = 60;

    public InMemoryRateLimitService(IConfiguration configuration, ILogger<InMemoryRateLimitService> logger)
    {
        _requestCounts = new ConcurrentDictionary<string, RateLimitEntry>();
        _logger = logger;

        LoadConfiguration(configuration);
    }

    /// <summary>
    /// Carga la configuración desde appsettings.json
    /// </summary>
    private void LoadConfiguration(IConfiguration configuration)
    {
        try
        {
            var rateLimitConfig = configuration.GetSection("RateLimiting");
            if (!rateLimitConfig.Exists())
            {
                _logger.LogWarning("RateLimiting section not found in configuration. Using default values.");
                return;
            }

            var policies = rateLimitConfig.GetSection("IpRateLimitPolicies");
            if (!policies.Exists())
            {
                _logger.LogWarning("IpRateLimitPolicies section not found. Using default values.");
                return;
            }

            // Cargar GeneralRule
            var generalRule = policies.GetSection("GeneralRule");
            if (generalRule.Exists())
            {
                if (int.TryParse(generalRule["Limit"], out var limit))
                    _generalLimit = limit;
                if (int.TryParse(generalRule["Period"]?.Replace("m", ""), out var period))
                    _windowSeconds = period * 60;
            }

            // Cargar LoginRule
            var loginRule = policies.GetSection("LoginRule");
            if (loginRule.Exists())
            {
                if (int.TryParse(loginRule["Limit"], out var limit))
                    _loginLimit = limit;
            }

            // Cargar RefreshRule
            var refreshRule = policies.GetSection("RefreshRule");
            if (refreshRule.Exists())
            {
                if (int.TryParse(refreshRule["Limit"], out var limit))
                    _refreshLimit = limit;
            }

            _logger.LogInformation(
                "Rate limiting configured - General: {General}, Login: {Login}, Refresh: {Refresh}, Window: {Window}s",
                _generalLimit, _loginLimit, _refreshLimit, _windowSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading rate limiting configuration. Using default values.");
        }
    }

    /// <summary>
    /// Determina el límite basado en el endpoint
    /// </summary>
    private int GetLimitForEndpoint(string endpoint)
    {
        if (endpoint.Contains("login", StringComparison.OrdinalIgnoreCase))
            return _loginLimit;
        
        if (endpoint.Contains("refresh", StringComparison.OrdinalIgnoreCase))
            return _refreshLimit;
        
        return _generalLimit;
    }

    public bool IsRequestAllowed(string clientId, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(endpoint))
            return true;

        var key = $"{clientId}:{endpoint}";
        var now = DateTime.UtcNow;
        var limit = GetLimitForEndpoint(endpoint);

        var entry = _requestCounts.AddOrUpdate(
            key,
            new RateLimitEntry { Count = 1, ResetTime = now.AddSeconds(_windowSeconds) },
            (k, existing) =>
            {
                if (now >= existing.ResetTime)
                {
                    return new RateLimitEntry { Count = 1, ResetTime = now.AddSeconds(_windowSeconds) };
                }
                existing.Count++;
                return existing;
            });

        if (entry.Count > limit)
        {
            _logger.LogWarning(
                "Rate limit exceeded for client {ClientId} on endpoint {Endpoint}. Limit: {Limit}, Count: {Count}",
                clientId, endpoint, limit, entry.Count);
            return false;
        }

        return true;
    }

    public (int Limit, int Remaining, int ResetInSeconds) GetRateLimitInfo(string clientId, string endpoint)
    {
        var key = $"{clientId}:{endpoint}";
        var limit = GetLimitForEndpoint(endpoint);

        if (_requestCounts.TryGetValue(key, out var entry))
        {
            var now = DateTime.UtcNow;
            var remaining = Math.Max(0, limit - entry.Count);
            var resetInSeconds = Math.Max(0, (int)(entry.ResetTime - now).TotalSeconds);
            return (limit, remaining, resetInSeconds);
        }

        return (limit, limit, _windowSeconds);
    }

    /// <summary>
    /// Limpia entradas expiradas de la memoria periódicamente.
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

    private class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTime ResetTime { get; set; }
    }
}
