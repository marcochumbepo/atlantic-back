namespace AtlanticOrders.Domain.Entities;

/// <summary>
/// Entidad para almacenar refresh tokens en la base de datos.
/// Permite renovar el access token sin re-autenticar.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Identificador único del refresh token.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Email del usuario propietario del token.
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Token generado aleatoriamente y almacenado de forma segura.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de expiración del refresh token.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Fecha de creación del token.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicador de si el token ha sido revocado.
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// Verifica si el token aún es válido (no expirado y no revocado).
    /// </summary>
    public bool IsValid => !IsRevoked && ExpiresAt > DateTime.UtcNow;
}
