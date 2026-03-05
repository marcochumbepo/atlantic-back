namespace AtlanticOrders.Application.DTOs;

/// <summary>
/// DTO para solicitud de login.
/// </summary>
public class LoginRequestDto
{
    public string email { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
}

/// <summary>
/// DTO interno para resultado de generación de token.
/// </summary>
public class TokenResultDto
{
    /// <summary>
    /// Token JWT generado para autenticación.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Tiempo de expiración del token en segundos.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Token de refresco para renovar el access token sin re-autenticar.
    /// </summary>
    public string? RefreshToken { get; set; }
}

/// <summary>
/// DTO para respuesta de login con token JWT y refresh token.
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// Token JWT generado para autenticación.
    /// Válido por el tiempo especificado en ExpiresIn.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Tiempo de expiración del token en segundos.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Token de refresco para renovar el access token.
    /// Se usa en el endpoint /api/auth/refresh.
    /// Válido por más tiempo que el access token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// DTO para solicitud de renovación de token.
/// Se envía junto con el refresh token para obtener un nuevo access token.
/// </summary>
public class RefreshTokenRequestDto
{
    /// <summary>
    /// Refresh token obtenido en el login.
    /// Debe ser válido y no estar revocado.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}

