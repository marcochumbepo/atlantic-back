using BCrypt.Net;

namespace AtlanticOrders.Infrastructure.Security;

/// <summary>
/// Interfaz para el servicio de hashing de contraseñas.
/// Define métodos para hashear y verificar contraseñas de forma segura.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashea una contraseña usando BCrypt.
    /// </summary>
    /// <param name="password">Contraseña en texto plano.</param>
    /// <returns>Contraseña hasheada y segura.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifica si una contraseña coincide con su hash.
    /// </summary>
    /// <param name="password">Contraseña en texto plano para verificar.</param>
    /// <param name="hash">Hash almacenado en la base de datos.</param>
    /// <returns>True si la contraseña coincide con el hash, false en caso contrario.</returns>
    bool VerifyPassword(string password, string hash);
}

/// <summary>
/// Implementación del servicio de hashing de contraseñas usando BCrypt.
/// BCrypt es un algoritmo criptográfico diseñado específicamente para hashear contraseñas.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Factor de trabajo de BCrypt (más alto = más seguro pero más lento).
    /// 12 es el balance recomendado entre seguridad y performance.
    /// Rango válido: 4-31.
    /// </summary>
    private const int WorkFactor = 12;

    /// <summary>
    /// Hashea una contraseña usando BCrypt con un factor de trabajo configurado.
    /// El hash incluye un salt generado aleatoriamente, por lo que cada contraseña
    /// hasheada es única aunque la contraseña sea la misma.
    /// </summary>
    /// <param name="password">Contraseña en texto plano.</param>
    /// <returns>Contraseña hasheada en formato BCrypt.</returns>
    /// <exception cref="ArgumentException">Si la contraseña es nula o vacía.</exception>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// Verifica si una contraseña coincide con su hash BCrypt.
    /// Usa el algoritmo de comparación seguro de BCrypt que previene timing attacks.
    /// </summary>
    /// <param name="password">Contraseña en texto plano para verificar.</param>
    /// <param name="hash">Hash almacenado en la base de datos.</param>
    /// <returns>True si la contraseña coincide exactamente con el hash, false en caso contrario.</returns>
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // Si hay algún error en la verificación, retornar false
            return false;
        }
    }
}
