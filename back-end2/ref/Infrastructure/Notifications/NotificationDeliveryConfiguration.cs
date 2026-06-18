using Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Notifications;

internal sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("notification_deliveries");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.NotificationId)
            .IsRequired();

        builder.HasIndex(d => d.NotificationId);

        builder.Property(d => d.Channel)
            .IsRequired();

        builder.Property(d => d.Status)
            .IsRequired();

        // Index for retry logic
        builder.HasIndex(d => new { d.Status, d.NextRetryAt });

        builder.Property(d => d.FailureReason)
            .HasMaxLength(500);

        builder.Property(d => d.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(d => d.ExternalId)
            .HasMaxLength(200);

        builder.Property(d => d.CreatedAt)
            .IsRequired();
    }
}
