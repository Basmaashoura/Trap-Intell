using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trap_Intel.Domain.Notifications;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Notifications;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", "trapintel");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId).IsRequired();
        builder.HasIndex(n => n.UserId);

        builder.Property(n => n.Type).IsRequired().HasMaxLength(100);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(255);
        builder.Property(n => n.Message).HasMaxLength(2000);
        builder.Property(n => n.LinkUri).HasMaxLength(1000);
        builder.Property(n => n.RelatedEntityId).HasMaxLength(100);

        builder.Property(n => n.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(n => n.Priority)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.IsRead).IsRequired();
        builder.Property(n => n.IsDismissed).IsRequired();

        // Indexes for fast dashboard loading
        builder.HasIndex(n => new { n.UserId, n.IsRead, n.IsDismissed });
        builder.HasIndex(n => n.CreatedAt);

        builder.HasOne<Trap_Intel.Domain.Identity.User>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
