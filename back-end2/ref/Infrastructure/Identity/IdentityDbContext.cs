using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

/// <summary>
/// Separate DbContext for ASP.NET Identity.
/// Uses 'identity' schema to keep tables separate from domain tables.
/// </summary>
public sealed class IdentityDbContext : IdentityDbContext<
    ApplicationIdentityUser,
    ApplicationIdentityRole,
    Guid,
    IdentityUserClaim<Guid>,
    IdentityUserRole<Guid>,
    IdentityUserLogin<Guid>,
    IdentityRoleClaim<Guid>,
    IdentityUserToken<Guid>>
{
    public const string SchemaName = "identity";

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema(SchemaName);

        ConfigureIdentityUser(builder);
        ConfigureIdentityRole(builder);
        ConfigureIdentityTables(builder);
    }

    private static void ConfigureIdentityUser(ModelBuilder builder)
    {
        builder.Entity<ApplicationIdentityUser>(entity =>
        {
            entity.ToTable("users");

            entity.Property(u => u.DomainUserId)
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            // SECURITY: Refresh token is now hashed
            entity.Property(u => u.RefreshTokenHash)
                .HasMaxLength(500)
                .HasColumnName("refresh_token_hash");

            entity.Property(u => u.RefreshTokenExpiresAt)
                .HasColumnName("refresh_token_expires_at");

            entity.Property(u => u.RefreshTokenCreatedAt)
                .HasColumnName("refresh_token_created_at");

            entity.HasIndex(u => u.DomainUserId)
                .IsUnique();

            // Index on refresh token hash for lookup performance
            entity.HasIndex(u => u.RefreshTokenHash)
                .HasFilter("refresh_token_hash IS NOT NULL");
        });
    }

    private static void ConfigureIdentityRole(ModelBuilder builder)
    {
        builder.Entity<ApplicationIdentityRole>(entity =>
        {
            entity.ToTable("roles");

            entity.Property(r => r.DomainRoleId)
                .IsRequired();

            entity.Property(r => r.CreatedAt)
                .IsRequired();

            entity.HasIndex(r => r.DomainRoleId)
                .IsUnique();
        });
    }

    private static void ConfigureIdentityTables(ModelBuilder builder)
    {
        builder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("user_claims");
        });

        builder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("user_roles");
        });

        builder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("user_logins");
        });

        builder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("role_claims");
        });

        builder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("user_tokens");
        });
    }
}
