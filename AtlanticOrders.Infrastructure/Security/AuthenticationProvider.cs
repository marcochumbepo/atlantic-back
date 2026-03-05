using AtlanticOrders.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AtlanticOrders.Infrastructure.Security;

/// <summary>
/// Proveedor de credenciales para autenticación.
/// Valida credenciales contra usuarios en la base de datos con contraseñas hasheadas.
/// </summary>
public interface IAuthenticationProvider
{
    Task<bool> ValidarCredencialesAsync(string email, string password);
}

/// <summary>
/// Implementación de autenticación que valida contra la base de datos.
/// Las contraseñas se verifican usando BCrypt de forma segura.
/// </summary>
public class AuthenticationProvider : IAuthenticationProvider
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthenticationProvider> _logger;

    public AuthenticationProvider(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILogger<AuthenticationProvider> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Valida credenciales de un usuario contra la base de datos.
    /// La contraseña se verifica de forma segura usando BCrypt.
    /// NO se loguea la contraseña por razones de seguridad.
    /// </summary>
    public async Task<bool> ValidarCredencialesAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Intento de login con credenciales vacías");
            return false;
        }

        try
        {
            // Buscar usuario por email
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                _logger.LogWarning("Intento de login para usuario inexistente: {Email}", email);
                return false;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Intento de login para usuario inactivo: {Email}", email);
                return false;
            }

            // Verificar contraseña de forma segura con BCrypt
            var passwordMatch = _passwordHasher.VerifyPassword(password, user.PasswordHash);

            if (!passwordMatch)
            {
                _logger.LogWarning("Intento de login fallido (contraseña incorrecta) para usuario: {Email}", email);
                return false;
            }

            // Actualizar fecha de último login
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Login exitoso para usuario: {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar credenciales para usuario: {Email}", email);
            return false;
        }
    }
}
