using Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Notifications;

internal sealed class UserNotificationPreferencesConfiguration : IEntityTypeConfiguration<UserNotificationPreferences>
{
    public void Configure(EntityTypeBuilder<UserNotificationPreferences> builder)
    {
        builder.ToTable("user_notification_preferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.InAppEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.EmailEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.PushEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.SmsEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.EmailDigestEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.EmailDigestFrequency)
            .IsRequired()
            .HasDefaultValue(DigestFrequency.Daily);

        builder.Property(p => p.QuietHoursEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.QuietHoursTimezone)
            .HasMaxLength(50);

        builder.Property(p => p.UpdatedAt)
            .IsRequired();
    }
}
