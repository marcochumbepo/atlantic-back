using AtlanticOrders.Domain.Exceptions;

namespace AtlanticOrders.Api.Middleware;

/// <summary>
/// Middleware de manejo global de excepciones.
/// Captura todas las excepciones no manejadas y devuelve respuestas HTTP apropiadas.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Excepción no manejada: {ExceptionMessage}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case NumeroPedidoDuplicadoException duplicadoEx:
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                response.Mensaje = duplicadoEx.Message;
                response.Codigo = "NUMERO_PEDIDO_DUPLICADO";
                break;

            case TotalInvalidoException totalEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Mensaje = totalEx.Message;
                response.Codigo = "TOTAL_INVALIDO";
                break;

            case DomainException domainEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Mensaje = domainEx.Message;
                response.Codigo = "ERROR_DOMINIO";
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Mensaje = "Ocurrió un error interno en el servidor.";
                response.Codigo = "ERROR_INTERNO";
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }

    /// <summary>
    /// Estructura de respuesta de error.
    /// </summary>
    private class ErrorResponse
    {
        public string Codigo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }
}
