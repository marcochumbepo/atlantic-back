namespace AtlanticOrders.Application.DTOs;

/// <summary>
/// DTO para respuestas de datos de Pedido.
/// </summary>
public class PedidoReadDto
{
    public int Id { get; set; }
    public string NumeroPedido { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public DateTime? Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
}

/// <summary>
/// DTO para crear un nuevo Pedido.
/// </summary>
public class CrearPedidoDto
{
    public string NumeroPedido { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public DateTime? Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "Pendiente";
}

/// <summary>
/// DTO para actualizar un Pedido.
/// </summary>
public class ActualizarPedidoDto
{
    public int Id { get; set; }
    public string NumeroPedido { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public DateTime? Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
}
