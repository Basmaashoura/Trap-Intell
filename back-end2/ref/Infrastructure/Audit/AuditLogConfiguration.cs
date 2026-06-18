using Domain.AuditLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Audit;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .IsRequired(false);

        builder.Property(a => a.UserEmail)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(a => a.Action)
            .IsRequired();

        builder.Property(a => a.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(a => a.EntityName)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(a => a.OldValues)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(a => a.NewValues)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(a => a.Description)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(a => a.Timestamp)
            .IsRequired();

        builder.Property(a => a.CorrelationId)
            .HasMaxLength(100)
            .IsRequired(false);

        // Indexes for common queries
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.EntityType);
        builder.HasIndex(a => a.Action);
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}
