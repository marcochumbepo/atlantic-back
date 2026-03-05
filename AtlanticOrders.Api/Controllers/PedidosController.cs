using Microsoft.AspNetCore.Mvc;
using AtlanticOrders.Application.DTOs;
using AtlanticOrders.Application.Services;
using Microsoft.AspNetCore.Authorization;

namespace AtlanticOrders.Api.Controllers;

/// <summary>
/// Controlador para operaciones CRUD de Pedidos.
/// Todos los endpoints requieren autenticación JWT.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PedidosController : ControllerBase
{
    private readonly IPedidoService _pedidoService;
    private readonly ILogger<PedidosController> _logger;

    public PedidosController(IPedidoService pedidoService, ILogger<PedidosController> logger)
    {
        _pedidoService = pedidoService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los pedidos activos.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<PedidoReadDto>>> ObtenerTodos()
    {
        _logger.LogInformation("Obteniendo todos los pedidos");
        var pedidos = await _pedidoService.ObtenerTodosAsync();
        return Ok(pedidos);
    }

    /// <summary>
    /// Obtiene un pedido específico por su ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PedidoReadDto>> ObtenerPorId(int id)
    {
        _logger.LogInformation("Obteniendo pedido con ID: {PedidoId}", id);
        var pedido = await _pedidoService.ObtenerPorIdAsync(id);

        if (pedido == null)
        {
            _logger.LogWarning("Pedido con ID {PedidoId} no encontrado", id);
            return NotFound(new { mensaje = $"El pedido con ID {id} no existe." });
        }

        return Ok(pedido);
    }

    /// <summary>
    /// Crea un nuevo pedido.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PedidoReadDto>> Crear([FromBody] CrearPedidoDto dto)
    {
        _logger.LogInformation("Creando nuevo pedido: {NumeroPedido}", dto.NumeroPedido);

        var pedido = await _pedidoService.CrearAsync(dto);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = pedido.Id }, pedido);
    }

    /// <summary>
    /// Actualiza un pedido existente.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PedidoReadDto>> Actualizar(int id, [FromBody] ActualizarPedidoDto dto)
    {
        if (id != dto.Id)
        {
            _logger.LogWarning("ID en URL ({UrlId}) no coincide con ID en body ({BodyId})", id, dto.Id);
            return BadRequest(new { mensaje = "El ID del pedido en la URL no coincide con el del body." });
        }

        _logger.LogInformation("Actualizando pedido con ID: {PedidoId}", id);
        var pedido = await _pedidoService.ActualizarAsync(dto);
        return Ok(pedido);
    }

    /// <summary>
    /// Elimina un pedido (soft delete).
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Eliminar(int id)
    {
        _logger.LogInformation("Eliminando pedido con ID: {PedidoId}", id);
        await _pedidoService.EliminarAsync(id);
        return NoContent();
    }
}
