using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trap_Intel.Domain.Identity.Entities;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// EF Core configuration for EmailVerificationToken entity.
/// </summary>
public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("EmailVerificationTokens", "trapintel");

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

        // Index for token lookup
        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_EmailVerificationTokens_TokenHash");

        // Index for user's tokens
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_EmailVerificationTokens_UserId");

        // Composite index for active token queries
        builder.HasIndex(x => new { x.UserId, x.IsRevoked, x.IsUsed, x.ExpiresAt })
            .HasDatabaseName("IX_EmailVerificationTokens_ActiveTokens");

        // Index for cleanup queries
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_EmailVerificationTokens_ExpiresAt");

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
