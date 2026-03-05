namespace AtlanticOrders.Api.Middleware;

/// <summary>
/// Middleware para implementar rate limiting en la API.
/// Limita el número de solicitudes que un cliente puede hacer en un período específico.
/// Es especialmente importante en endpoints como login para prevenir ataques de fuerza bruta.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Procesa la solicitud y aplica rate limiting.
    /// Obtiene la IP del cliente e intenta procesar la solicitud.
    /// Si se excede el límite, retorna 429 Too Many Requests.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IRateLimitService rateLimitService)
    {
        // Obtener IP del cliente
        var clientIp = GetClientIp(context);
        
        // Construir identificador del endpoint (ej: POST:/api/auth/login)
        var endpoint = $"{context.Request.Method}:{context.Request.Path}";

        _logger.LogDebug(
            "Rate limit check for client {ClientIp} on endpoint {Endpoint}",
            clientIp, endpoint);

        // Verificar si la solicitud está permitida
        if (!rateLimitService.IsRequestAllowed(clientIp, endpoint))
        {
            _logger.LogWarning(
                "Rate limit exceeded for client {ClientIp} on endpoint {Endpoint}",
                clientIp, endpoint);

            // Obtener información del rate limit
            var (limit, remaining, resetInSeconds) = rateLimitService.GetRateLimitInfo(clientIp, endpoint);

            // Configurar headers de respuesta
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = resetInSeconds.ToString();
            context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow
                .AddSeconds(resetInSeconds)
                .ToUnixTimeSeconds()
                .ToString();

            // Escribir respuesta de error
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Demasiadas solicitudes. Por favor, intente más tarde.",
                message = $"Ha excedido el límite de {limit} solicitudes por minuto.",
                retryAfter = resetInSeconds,
                limit = limit,
                remaining = remaining,
                resetInSeconds = resetInSeconds
            });

            return;
        }

        // Obtener información del rate limit para enviar en headers
        var (currentLimit, currentRemaining, currentReset) = rateLimitService.GetRateLimitInfo(clientIp, endpoint);
        context.Response.Headers["X-RateLimit-Limit"] = currentLimit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = currentRemaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow
            .AddSeconds(currentReset)
            .ToUnixTimeSeconds()
            .ToString();

        // Continuar con la siguiente solicitud
        await _next(context);
    }

    /// <summary>
    /// Obtiene la IP del cliente considerando proxies (X-Forwarded-For).
    /// Útil cuando la aplicación está detrás de un load balancer o proxy.
    /// </summary>
    private string GetClientIp(HttpContext context)
    {
        // Intentar obtener IP del header X-Forwarded-For (para proxies)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ips = forwardedFor.ToString().Split(',');
            if (ips.Length > 0 && !string.IsNullOrWhiteSpace(ips[0]))
            {
                return ips[0].Trim();
            }
        }

        // Fallback a RemoteIpAddress
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// Extensión para registrar el middleware de rate limiting fácilmente.
/// </summary>
public static class RateLimitingExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitMiddleware>();
    }
}
