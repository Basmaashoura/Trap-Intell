using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trap_Intel.Domain.Notifications;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Notifications;

internal sealed class UserPushTokenConfiguration : IEntityTypeConfiguration<UserPushToken>
{
    public void Configure(EntityTypeBuilder<UserPushToken> builder)
    {
        builder.ToTable("UserPushTokens", "trapintel");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).IsRequired();
        builder.HasIndex(t => t.UserId);

        builder.Property(t => t.Token).IsRequired().HasMaxLength(500);
        builder.HasIndex(t => t.Token).IsUnique(); // Prevent duplicate tokens system-wide

        builder.Property(t => t.Platform)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.DeviceId).HasMaxLength(150);

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.LastUsedAt).IsRequired();

        builder.HasOne<Trap_Intel.Domain.Identity.User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
