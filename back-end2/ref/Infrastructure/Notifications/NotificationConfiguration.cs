using Domain.Notifications;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Notifications;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId)
            .IsRequired();

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(n => n.TypeId)
            .IsRequired();

        builder.HasOne(n => n.Type)
            .WithMany()
            .HasForeignKey(n => n.TypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(n => n.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(n => n.Priority)
            .IsRequired();

        builder.Property(n => n.EntityType)
            .HasMaxLength(50);

        builder.HasIndex(n => new { n.EntityType, n.EntityId });

        builder.Property(n => n.ActionUrl)
            .HasMaxLength(500);

        builder.Property(n => n.ActionText)
            .HasMaxLength(100);

        builder.Property(n => n.Metadata)
            .HasColumnType("text");

        builder.Property(n => n.GroupKey)
            .HasMaxLength(100);

        builder.HasIndex(n => n.GroupKey)
            .HasFilter("group_key IS NOT NULL");

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.IsDismissed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        // Index for unread notifications (most common query)
        builder.HasIndex(n => new { n.UserId, n.IsRead })
            .HasFilter("is_read = false");

        // Index for user notifications sorted by date
        builder.HasIndex(n => new { n.UserId, n.CreatedAt });

        // Index for scheduled notifications
        builder.HasIndex(n => n.ScheduledFor)
            .HasFilter("scheduled_for IS NOT NULL");

        // Index for expiring notifications
        builder.HasIndex(n => n.ExpiresAt)
            .HasFilter("expires_at IS NOT NULL");

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.HasOne(n => n.CreatedBy)
            .WithMany()
            .HasForeignKey(n => n.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(n => n.Deliveries)
            .WithOne(d => d.Notification)
            .HasForeignKey(d => d.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
