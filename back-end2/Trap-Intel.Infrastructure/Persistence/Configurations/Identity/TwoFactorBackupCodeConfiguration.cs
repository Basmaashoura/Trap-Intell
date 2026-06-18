using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trap_Intel.Domain.Identity.Entities;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// EF Core configuration for TwoFactorBackupCode entity.
/// </summary>
public class TwoFactorBackupCodeConfiguration : IEntityTypeConfiguration<TwoFactorBackupCode>
{
    public void Configure(EntityTypeBuilder<TwoFactorBackupCode> builder)
    {
        builder.ToTable("TwoFactorBackupCodes", "trapintel");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.CodeHash)
            .HasMaxLength(64) // SHA-256 produces 32 bytes = 64 hex chars
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.UsedAt);

        builder.Property(x => x.UsedFromIp)
            .HasMaxLength(45); // IPv6 max length

        // Index for finding codes by user
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_TwoFactorBackupCodes_UserId");

        // Composite index for code lookup (user + hash + not used)
        builder.HasIndex(x => new { x.UserId, x.CodeHash, x.IsUsed })
            .HasDatabaseName("IX_TwoFactorBackupCodes_Lookup");

        // Index for cleanup queries (find used codes)
        builder.HasIndex(x => new { x.IsUsed, x.UsedAt })
            .HasDatabaseName("IX_TwoFactorBackupCodes_Cleanup")
            .HasFilter("\"IsUsed\" = true");

        // Relationship with User
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
