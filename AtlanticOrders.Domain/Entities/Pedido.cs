namespace AtlanticOrders.Domain.Entities;

/// <summary>
/// Entidad Pedido - Representa una orden de compra en el sistema.
/// </summary>
public class Pedido
{
    public int Id { get; set; }

    /// <summary>
    /// Número único del pedido.
    /// </summary>
    public string NumeroPedido { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del cliente que realizó el pedido.
    /// </summary>
    public string Cliente { get; set; } = string.Empty;

    /// <summary>
    /// Fecha en que se realizó el pedido.
    /// </summary>
    public DateTime Fecha { get; set; }

    /// <summary>
    /// Monto total del pedido.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Estado actual del pedido (ej: Pendiente, Confirmado, Entregado).
    /// </summary>
    public string Estado { get; set; } = "Pendiente";

    /// <summary>
    /// Indica si el registro ha sido eliminado lógicamente (soft delete).
    /// </summary>
    public bool EliminadoLogicamente { get; set; } = false;

    /// <summary>
    /// Fecha de eliminación lógica.
    /// </summary>
    public DateTime? FechaEliminacion { get; set; }
}
