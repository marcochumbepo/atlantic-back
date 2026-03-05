using AtlanticOrders.Domain.Entities;

namespace AtlanticOrders.Domain.Repositories;

/// <summary>
/// Interfaz del repositorio para gestionar operaciones CRUD de usuarios.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Obtiene un usuario por su email.
    /// </summary>
    /// <param name="email">Email del usuario a buscar.</param>
    /// <returns>Usuario encontrado o null si no existe.</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Obtiene un usuario por su ID.
    /// </summary>
    /// <param name="id">ID del usuario.</param>
    /// <returns>Usuario encontrado o null si no existe.</returns>
    Task<User?> GetByIdAsync(int id);

    /// <summary>
    /// Crea un nuevo usuario.
    /// </summary>
    /// <param name="user">Usuario a crear.</param>
    /// <returns>Usuario creado con ID asignado.</returns>
    Task<User> CreateAsync(User user);

    /// <summary>
    /// Actualiza un usuario existente.
    /// </summary>
    /// <param name="user">Usuario con datos actualizados.</param>
    Task UpdateAsync(User user);

    /// <summary>
    /// Verifica si un usuario existe por email.
    /// </summary>
    /// <param name="email">Email a verificar.</param>
    /// <returns>True si el usuario existe, false en caso contrario.</returns>
    Task<bool> ExistsByEmailAsync(string email);
}

/// <summary>
/// Interfaz del repositorio para gestionar operaciones CRUD de refresh tokens.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Obtiene un refresh token válido por su token string.
    /// </summary>
    /// <param name="token">Token a buscar.</param>
    /// <returns>Refresh token encontrado y válido, o null.</returns>
    Task<RefreshToken?> GetByTokenAsync(string token);

    /// <summary>
    /// Obtiene todos los refresh tokens válidos de un usuario.
    /// </summary>
    /// <param name="userEmail">Email del usuario.</param>
    /// <returns>Lista de refresh tokens válidos del usuario.</returns>
    Task<IEnumerable<RefreshToken>> GetValidTokensByUserAsync(string userEmail);

    /// <summary>
    /// Crea un nuevo refresh token.
    /// </summary>
    /// <param name="token">Refresh token a crear.</param>
    /// <returns>Refresh token creado con ID asignado.</returns>
    Task<RefreshToken> CreateAsync(RefreshToken token);

    /// <summary>
    /// Revoca un refresh token (lo marca como revocado).
    /// </summary>
    /// <param name="token">Token a revocar.</param>
    Task RevokeAsync(RefreshToken token);

    /// <summary>
    /// Revoca todos los refresh tokens de un usuario.
    /// Útil en caso de cambio de contraseña o logout de todos los dispositivos.
    /// </summary>
    /// <param name="userEmail">Email del usuario.</param>
    Task RevokeAllForUserAsync(string userEmail);

    /// <summary>
    /// Elimina tokens expirados (limpieza de BD).
    /// </summary>
    /// <returns>Cantidad de tokens eliminados.</returns>
    Task<int> DeleteExpiredTokensAsync();
}
