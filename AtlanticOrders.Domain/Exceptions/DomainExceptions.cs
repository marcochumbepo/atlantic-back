namespace AtlanticOrders.Domain.Exceptions;

/// <summary>
/// Excepción base para errores de validación de negocio.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

/// <summary>
/// Excepción para cuando un número de pedido ya existe.
/// </summary>
public class NumeroPedidoDuplicadoException : DomainException
{
    public NumeroPedidoDuplicadoException(string numeroPedido)
        : base($"El número de pedido '{numeroPedido}' ya existe en el sistema.") { }
}

/// <summary>
/// Excepción para cuando un total de pedido es inválido.
/// </summary>
public class TotalInvalidoException : DomainException
{
    public TotalInvalidoException()
        : base("El total del pedido debe ser mayor a 0.") { }
}
