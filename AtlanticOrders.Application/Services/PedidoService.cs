using AtlanticOrders.Application.DTOs;
using AtlanticOrders.Domain.Repositories;
using AtlanticOrders.Application.Common.Helpers;
using AutoMapper;
using AtlanticOrders.Domain.Exceptions;

namespace AtlanticOrders.Application.Services;

/// <summary>
/// Servicio de aplicación para operaciones relacionadas con Pedidos.
/// Contiene la lógica de negocio y coordina con el repositorio.
/// </summary>
public interface IPedidoService
{
    Task<IEnumerable<PedidoReadDto>> ObtenerTodosAsync();
    Task<PedidoReadDto?> ObtenerPorIdAsync(int id);
    Task<PedidoReadDto> CrearAsync(CrearPedidoDto dto);
    Task<PedidoReadDto> ActualizarAsync(ActualizarPedidoDto dto);
    Task EliminarAsync(int id);
}

/// <summary>
/// Implementación del servicio de Pedidos.
/// </summary>
public class PedidoService : IPedidoService
{
    private readonly IPedidoRepository _repository;
    private readonly IMapper _mapper;

    public PedidoService(IPedidoRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PedidoReadDto>> ObtenerTodosAsync()
    {
        var pedidos = await _repository.ObtenerTodosAsync();

        var result = _mapper.Map<IEnumerable<PedidoReadDto>>(pedidos);

        foreach (var pedido in result)
        {
            // Convert to Lima timezone only if Fecha has a value
            if (pedido.Fecha.HasValue)
            {
                pedido.Fecha = TimeZoneHelper.ConvertToLima(pedido.Fecha.Value);
            }
        }
        return _mapper.Map<IEnumerable<PedidoReadDto>>(result);
    }

    public async Task<PedidoReadDto?> ObtenerPorIdAsync(int id)
    {
        var pedido = await _repository.ObtenerPorIdAsync(id);
        if (pedido == null)
            return null;

        return _mapper.Map<PedidoReadDto>(pedido);
    }

    public async Task<PedidoReadDto> CrearAsync(CrearPedidoDto dto)
    {
        // Validar reglas de negocio
        if (dto.Total <= 0)
            throw new TotalInvalidoException();

        if (await _repository.ExisteNumeroPedidoAsync(dto.NumeroPedido))
            throw new NumeroPedidoDuplicadoException(dto.NumeroPedido);

        var pedido = _mapper.Map<Domain.Entities.Pedido>(dto);
        pedido.Fecha = DateTime.UtcNow;

        var pedidoCreado = await _repository.CrearAsync(pedido);
        return _mapper.Map<PedidoReadDto>(pedidoCreado);
    }

    public async Task<PedidoReadDto> ActualizarAsync(ActualizarPedidoDto dto)
    {
        // Validar reglas de negocio
        if (dto.Total <= 0)
            throw new TotalInvalidoException();

        var pedidoExistente = await _repository.ObtenerPorIdAsync(dto.Id);
        if (pedidoExistente == null)
            throw new DomainException($"El pedido con ID {dto.Id} no existe.");

        // Validar que el número de pedido sea único (excluir el actual)
        if (pedidoExistente.NumeroPedido != dto.NumeroPedido && 
            await _repository.ExisteNumeroPedidoAsync(dto.NumeroPedido, dto.Id))
            throw new NumeroPedidoDuplicadoException(dto.NumeroPedido);

        // Actualizar propiedades del entity existente
        pedidoExistente.NumeroPedido = dto.NumeroPedido;
        pedidoExistente.Cliente = dto.Cliente;
        pedidoExistente.Total = dto.Total;
        pedidoExistente.Estado = dto.Estado;

        var pedidoActualizado = await _repository.ActualizarAsync(pedidoExistente);
        return _mapper.Map<PedidoReadDto>(pedidoActualizado);
    }

    public async Task EliminarAsync(int id)
    {
        var pedido = await _repository.ObtenerPorIdAsync(id);
        if (pedido == null)
            throw new DomainException($"El pedido con ID {id} no existe.");

        await _repository.EliminarLogicamenteAsync(id);
    }
}
