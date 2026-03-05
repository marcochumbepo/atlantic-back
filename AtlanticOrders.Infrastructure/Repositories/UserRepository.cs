using Microsoft.EntityFrameworkCore;
using AtlanticOrders.Domain.Entities;
using AtlanticOrders.Domain.Repositories;
using AtlanticOrders.Infrastructure.Persistence;

namespace AtlanticOrders.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de usuarios.
/// Gestiona todas las operaciones CRUD de usuarios en la base de datos.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AtlanticOrdersDbContext _context;

    public UserRepository(AtlanticOrdersDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene un usuario por su email.
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.ToLower());
    }

    /// <summary>
    /// Obtiene un usuario por su ID.
    /// </summary>
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <summary>
    /// Crea un nuevo usuario.
    /// </summary>
    public async Task<User> CreateAsync(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        // Normalizar email a minúsculas para búsquedas consistentes
        user.Email = user.Email.ToLower();
        user.CreatedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Actualiza un usuario existente.
    /// </summary>
    public async Task UpdateAsync(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Verifica si un usuario existe por email.
    /// </summary>
    public async Task<bool> ExistsByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return await _context.Users
            .AnyAsync(u => u.Email == email.ToLower());
    }
}
