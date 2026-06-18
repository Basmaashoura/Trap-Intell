using Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Notifications;

internal sealed class UserNotificationTypeSettingConfiguration : IEntityTypeConfiguration<UserNotificationTypeSetting>
{
    public void Configure(EntityTypeBuilder<UserNotificationTypeSetting> builder)
    {
        builder.ToTable("user_notification_type_settings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.TypeId)
            .IsRequired();

        // Unique constraint on user + type
        builder.HasIndex(s => new { s.UserId, s.TypeId })
            .IsUnique();

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Type)
            .WithMany()
            .HasForeignKey(s => s.TypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.UpdatedAt)
            .IsRequired();
    }
}
