using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AtlanticOrders.Application.DTOs;

namespace AtlanticOrders.Infrastructure.Security;

/// <summary>
/// Interfaz para generar tokens JWT y refresh tokens.
/// </summary>
public interface ITokenProvider
{
    /// <summary>
    /// Genera un token JWT para un usuario.
    /// </summary>
    TokenResultDto GenerarToken(string usuario, string rol = "User");

    /// <summary>
    /// Genera un refresh token aleatorio y seguro.
    /// El refresh token es usado para renovar el access token sin re-autenticar.
    /// </summary>
    string GenerarRefreshToken();

    /// <summary>
    /// Obtiene el principal (claims) de un token JWT expirado.
    /// Útil para renovar tokens usando el refresh token.
    /// </summary>
    ClaimsPrincipal? ObtenerPrincipalDelTokenExpirado(string token);
}

/// <summary>
/// Proveedor de tokens JWT y refresh tokens.
/// Genera tokens firmados con una clave secreta y refresh tokens seguros.
/// </summary>
public class JwtTokenProvider : ITokenProvider
{
    private readonly string _jwtSecretKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpirationMinutes;

    public JwtTokenProvider(string jwtSecretKey, string jwtIssuer, string jwtAudience, int jwtExpirationMinutes)
    {
        _jwtSecretKey = jwtSecretKey;
        _jwtIssuer = jwtIssuer;
        _jwtAudience = jwtAudience;
        _jwtExpirationMinutes = jwtExpirationMinutes;
    }

    /// <summary>
    /// Genera un token JWT para un usuario.
    /// El token incluye el email, nombre y rol como claims.
    /// </summary>
    public TokenResultDto GenerarToken(string usuario, string rol = "User")
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario),
            new Claim(ClaimTypes.Name, usuario),
            new Claim(ClaimTypes.Role, rol)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Calcular expiresIn en segundos
        var expiresIn = (int)(expiresAt - DateTime.UtcNow).TotalSeconds;

        return new TokenResultDto
        {
            Token = tokenString,
            ExpiresIn = expiresIn
        };
    }

    /// <summary>
    /// Genera un refresh token aleatorio y seguro.
    /// Usa RNGCryptoServiceProvider para generación criptográfica.
    /// El refresh token se almacena en la base de datos y se usa para renovar el access token.
    /// </summary>
    public string GenerarRefreshToken()
    {
        var randomNumber = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
        }

        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Obtiene el principal (claims) de un token JWT expirado.
    /// Útil para renovar tokens: el refresh token permite generar un nuevo access token
    /// sin que el usuario tenga que re-autenticar.
    /// </summary>
    public ClaimsPrincipal? ObtenerPrincipalDelTokenExpirado(string token)
    {
        try
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecretKey)),
                ValidateLifetime = false // Ignorar expiración para obtener el principal
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (!(securityToken is JwtSecurityToken jwtSecurityToken) ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
