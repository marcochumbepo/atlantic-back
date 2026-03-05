using AtlanticOrders.Domain.Entities;

namespace AtlanticOrders.Domain.Repositories;

/// <summary>
/// Interfaz del repositorio para la entidad Pedido.
/// Define el contrato para las operaciones CRUD y consultas.
/// </summary>
public interface IPedidoRepository
{
    /// <summary>
    /// Obtiene todos los pedidos activos (no eliminados lógicamente).
    /// </summary>
    Task<IEnumerable<Pedido>> ObtenerTodosAsync();

    /// <summary>
    /// Obtiene un pedido específico por su ID.
    /// </summary>
    Task<Pedido?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Obtiene un pedido por su número único.
    /// </summary>
    Task<Pedido?> ObtenerPorNumeroPedidoAsync(string numeroPedido);

    /// <summary>
    /// Crea un nuevo pedido en la base de datos.
    /// </summary>
    Task<Pedido> CrearAsync(Pedido pedido);

    /// <summary>
    /// Actualiza un pedido existente.
    /// </summary>
    Task<Pedido> ActualizarAsync(Pedido pedido);

    /// <summary>
    /// Elimina lógicamente un pedido (marca como eliminado).
    /// </summary>
    Task EliminarLogicamenteAsync(int id);

    /// <summary>
    /// Verifica si existe un pedido con un número específico.
    /// </summary>
    Task<bool> ExisteNumeroPedidoAsync(string numeroPedido, int? excluirId = null);
}
