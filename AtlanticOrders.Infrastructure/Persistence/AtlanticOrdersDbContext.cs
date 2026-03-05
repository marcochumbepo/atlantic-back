using Microsoft.EntityFrameworkCore;
using AtlanticOrders.Domain.Entities;

namespace AtlanticOrders.Infrastructure.Persistence;

/// <summary>
/// DbContext de Entity Framework Core para la aplicación.
/// Define las configuraciones del modelo y las entidades.
/// </summary>
public class AtlanticOrdersDbContext : DbContext
{
    public AtlanticOrdersDbContext(DbContextOptions<AtlanticOrdersDbContext> options)
        : base(options)
    {
    }

    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de la entidad Pedido
        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.NumeroPedido)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Cliente)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Fecha)
                .IsRequired();

            entity.Property(e => e.Total)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.Estado)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Pendiente");

            entity.Property(e => e.EliminadoLogicamente)
                .IsRequired()
                .HasDefaultValue(false);

            // Índice único para NumeroPedido (solo considerar no eliminados)
            entity.HasIndex(e => e.NumeroPedido)
                .IsUnique()
                .HasFilter("[EliminadoLogicamente] = 0");

            // Global query filter para soft delete
            entity.HasQueryFilter(e => !e.EliminadoLogicamente);
        });

        // Configuración de la entidad User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.PasswordHash)
                .IsRequired();

            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("User");

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Índice único para Email
            entity.HasIndex(e => e.Email)
                .IsUnique();

            // Índice para búsquedas por Email
            entity.HasIndex(e => e.IsActive);
        });

        // Configuración de la entidad RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserEmail)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.ExpiresAt)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.IsRevoked)
                .IsRequired()
                .HasDefaultValue(false);

            // Índices para búsquedas rápidas
            entity.HasIndex(e => e.Token)
                .IsUnique();

            entity.HasIndex(e => new { e.UserEmail, e.IsRevoked });

            entity.HasIndex(e => e.ExpiresAt);
        });
    }
}
