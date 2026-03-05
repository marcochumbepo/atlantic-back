using Microsoft.EntityFrameworkCore;
using AtlanticOrders.Domain.Entities;
using AtlanticOrders.Domain.Repositories;
using AtlanticOrders.Infrastructure.Persistence;

namespace AtlanticOrders.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de Pedidos.
/// Maneja todas las operaciones CRUD contra la base de datos.
/// </summary>
public class PedidoRepository : IPedidoRepository
{
    private readonly AtlanticOrdersDbContext _context;

    public PedidoRepository(AtlanticOrdersDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Pedido>> ObtenerTodosAsync()
    {
        return await _context.Pedidos
            .OrderByDescending(p => p.Fecha)
            .ToListAsync();
    }

    public async Task<Pedido?> ObtenerPorIdAsync(int id)
    {
        return await _context.Pedidos
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Pedido?> ObtenerPorNumeroPedidoAsync(string numeroPedido)
    {
        return await _context.Pedidos
            .FirstOrDefaultAsync(p => p.NumeroPedido == numeroPedido);
    }

    public async Task<Pedido> CrearAsync(Pedido pedido)
    {
        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();
        return pedido;
    }

    public async Task<Pedido> ActualizarAsync(Pedido pedido)
    {
        _context.Pedidos.Update(pedido);
        await _context.SaveChangesAsync();
        return pedido;
    }

    public async Task EliminarLogicamenteAsync(int id)
    {
        var pedido = await ObtenerPorIdAsync(id);
        if (pedido != null)
        {
            pedido.EliminadoLogicamente = true;
            pedido.FechaEliminacion = DateTime.UtcNow;
            await ActualizarAsync(pedido);
        }
    }

    public async Task<bool> ExisteNumeroPedidoAsync(string numeroPedido, int? excluirId = null)
    {
        var query = _context.Pedidos.AsQueryable();

        // Incluir los eliminados lógicamente para esta verificación
        query = query.IgnoreQueryFilters();

        var existe = await query
            .AnyAsync(p => p.NumeroPedido == numeroPedido && 
                          !p.EliminadoLogicamente &&
                          (excluirId == null || p.Id != excluirId));

        return existe;
    }
}
