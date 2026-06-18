using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trap_Intel.Domain.Identity.Entities;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// EF Core configuration for PasswordResetToken entity.
/// </summary>
public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens", "trapintel");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasMaxLength(64) // SHA-256 produces 32 bytes = 64 hex chars
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.UsedAt);

        builder.Property(x => x.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.RevokedAt);

        builder.Property(x => x.RequestedFromIp)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(x => x.RequestedFromUserAgent)
            .HasMaxLength(500);

        builder.Property(x => x.UsedFromIp)
            .HasMaxLength(45);

        // Index for token lookup
        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_PasswordResetTokens_TokenHash");

        // Index for user's tokens
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_PasswordResetTokens_UserId");

        // Composite index for active token queries
        builder.HasIndex(x => new { x.UserId, x.IsRevoked, x.IsUsed, x.ExpiresAt })
            .HasDatabaseName("IX_PasswordResetTokens_ActiveTokens");

        // Index for cleanup queries
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_PasswordResetTokens_ExpiresAt");

        // Index for rate limiting queries
        builder.HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("IX_PasswordResetTokens_RateLimit");

        // Relationship with User
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed properties
        builder.Ignore(x => x.IsValid);
        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.TimeUntilExpiry);
    }
}
