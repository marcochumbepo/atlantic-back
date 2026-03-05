using AutoMapper;
using AtlanticOrders.Application.DTOs;
using AtlanticOrders.Domain.Entities;

namespace AtlanticOrders.Application.Mappings;

/// <summary>
/// Configuración de AutoMapper para mapear entre entidades y DTOs.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Mapeos de Pedido
        CreateMap<Pedido, PedidoReadDto>();
        CreateMap<CrearPedidoDto, Pedido>();
        CreateMap<ActualizarPedidoDto, Pedido>();
    }
}
