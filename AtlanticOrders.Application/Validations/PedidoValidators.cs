using FluentValidation;
using AtlanticOrders.Application.DTOs;

namespace AtlanticOrders.Application.Validations;

/// <summary>
/// Validador de FluentValidation para crear pedidos.
/// </summary>
public class CrearPedidoDtoValidator : AbstractValidator<CrearPedidoDto>
{
    public CrearPedidoDtoValidator()
    {
        RuleFor(x => x.NumeroPedido)
            .NotEmpty().WithMessage("El número de pedido es requerido.")
            .MinimumLength(1).WithMessage("El número de pedido debe tener al menos 1 carácter.")
            .MaximumLength(50).WithMessage("El número de pedido no puede exceder 50 caracteres.");

        RuleFor(x => x.Cliente)
            .NotEmpty().WithMessage("El nombre del cliente es requerido.")
            .MinimumLength(2).WithMessage("El nombre del cliente debe tener al menos 2 caracteres.")
            .MaximumLength(100).WithMessage("El nombre del cliente no puede exceder 100 caracteres.");

        RuleFor(x => x.Fecha)
            .NotEmpty().WithMessage("La fecha del pedido es requerida.")
            .LessThanOrEqualTo(DateTime.Now).WithMessage("La fecha no puede ser en el futuro.");

        RuleFor(x => x.Total)
            .GreaterThan(0).WithMessage("El total debe ser mayor a 0.");

        RuleFor(x => x.Estado)
            .NotEmpty().WithMessage("El estado del pedido es requerido.")
            .Must(x => new[] { "Pendiente", "Confirmado", "Entregado", "Cancelado" }.Contains(x))
            .WithMessage("El estado debe ser: Pendiente, Confirmado, Entregado o Cancelado.");
    }
}

/// <summary>
/// Validador de FluentValidation para actualizar pedidos.
/// </summary>
public class ActualizarPedidoDtoValidator : AbstractValidator<ActualizarPedidoDto>
{
    public ActualizarPedidoDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El ID del pedido debe ser válido.");

        RuleFor(x => x.NumeroPedido)
            .NotEmpty().WithMessage("El número de pedido es requerido.")
            .MinimumLength(1).WithMessage("El número de pedido debe tener al menos 1 carácter.")
            .MaximumLength(50).WithMessage("El número de pedido no puede exceder 50 caracteres.");

        RuleFor(x => x.Cliente)
            .NotEmpty().WithMessage("El nombre del cliente es requerido.")
            .MinimumLength(2).WithMessage("El nombre del cliente debe tener al menos 2 caracteres.")
            .MaximumLength(100).WithMessage("El nombre del cliente no puede exceder 100 caracteres.");

        RuleFor(x => x.Fecha)
            .NotEmpty().WithMessage("La fecha del pedido es requerida.")
            .LessThanOrEqualTo(DateTime.Now).WithMessage("La fecha no puede ser en el futuro.");

        RuleFor(x => x.Total)
            .GreaterThan(0).WithMessage("El total debe ser mayor a 0.");

        RuleFor(x => x.Estado)
            .NotEmpty().WithMessage("El estado del pedido es requerido.")
            .Must(x => new[] { "Pendiente", "Confirmado", "Entregado", "Cancelado" }.Contains(x))
            .WithMessage("El estado debe ser: Pendiente, Confirmado, Entregado o Cancelado.");
    }
}
