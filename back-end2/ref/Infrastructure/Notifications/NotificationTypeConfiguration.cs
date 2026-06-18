using Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Notifications;

internal sealed class NotificationTypeConfiguration : IEntityTypeConfiguration<NotificationType>
{
    public void Configure(EntityTypeBuilder<NotificationType> builder)
    {
        builder.ToTable("notification_types");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(t => t.Code)
            .IsUnique();

        builder.Property(t => t.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.Category)
            .IsRequired();

        builder.HasIndex(t => t.Category);

        builder.Property(t => t.DefaultPriority)
            .IsRequired();

        builder.Property(t => t.DefaultChannels)
            .IsRequired();

        builder.Property(t => t.IconName)
            .HasMaxLength(50);

        builder.Property(t => t.ColorHex)
            .HasMaxLength(7);

        builder.Property(t => t.TemplateTitle)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.TemplateBody)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.IsSystemType)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.CreatedAt)
            .IsRequired();
    }
}
