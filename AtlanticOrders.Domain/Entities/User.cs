namespace AtlanticOrders.Domain.Entities;

/// <summary>
/// Entidad para almacenar usuarios con contraseñas hasheadas en la base de datos.
/// Reemplaza el diccionario hardcodeado de usuarios.
/// </summary>
public class User
{
    /// <summary>
    /// Identificador único del usuario.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Email del usuario (único y usado para login).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña hasheada con BCrypt.
    /// NUNCA se almacena en plaintext.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del usuario.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Rol del usuario (Admin, User, etc.)
    /// </summary>
    public string Role { get; set; } = "User";

    /// <summary>
    /// Indica si la cuenta está activa.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Fecha de creación del usuario.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Última fecha en que se actualizó el usuario.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Última fecha en que se intentó login con este usuario.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}
