using Microsoft.EntityFrameworkCore;
using AtlanticOrders.Domain.Entities;
using AtlanticOrders.Domain.Repositories;
using AtlanticOrders.Infrastructure.Persistence;

namespace AtlanticOrders.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de refresh tokens.
/// Gestiona todas las operaciones CRUD de refresh tokens en la base de datos.
/// </summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AtlanticOrdersDbContext _context;

    public RefreshTokenRepository(AtlanticOrdersDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene un refresh token válido por su token string.
    /// Un token válido es aquel que no ha expirado y no ha sido revocado.
    /// </summary>
    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        return await _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt =>
                rt.Token == token &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow);
    }

    /// <summary>
    /// Obtiene todos los refresh tokens válidos de un usuario.
    /// </summary>
    public async Task<IEnumerable<RefreshToken>> GetValidTokensByUserAsync(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return Enumerable.Empty<RefreshToken>();

        return await _context.RefreshTokens
            .AsNoTracking()
            .Where(rt =>
                rt.UserEmail == userEmail.ToLower() &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }

    /// <summary>
    /// Crea un nuevo refresh token.
    /// </summary>
    public async Task<RefreshToken> CreateAsync(RefreshToken token)
    {
        if (token == null)
            throw new ArgumentNullException(nameof(token));

        token.UserEmail = token.UserEmail.ToLower();
        token.CreatedAt = DateTime.UtcNow;

        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();

        return token;
    }

    /// <summary>
    /// Revoca un refresh token (lo marca como revocado).
    /// </summary>
    public async Task RevokeAsync(RefreshToken token)
    {
        if (token == null)
            throw new ArgumentNullException(nameof(token));

        token.IsRevoked = true;
        _context.RefreshTokens.Update(token);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Revoca todos los refresh tokens de un usuario.
    /// Útil para logout de todos los dispositivos.
    /// </summary>
    public async Task RevokeAllForUserAsync(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return;

        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserEmail == userEmail.ToLower() && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }

        if (tokens.Any())
        {
            _context.RefreshTokens.UpdateRange(tokens);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Elimina tokens expirados de la base de datos (limpieza).
    /// Debe ejecutarse periódicamente para liberar espacio.
    /// </summary>
    public async Task<int> DeleteExpiredTokensAsync()
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (!expiredTokens.Any())
            return 0;

        _context.RefreshTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync();

        return expiredTokens.Count;
    }
}
