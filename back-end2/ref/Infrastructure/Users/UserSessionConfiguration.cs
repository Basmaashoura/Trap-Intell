using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Users;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.TokenHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.RefreshTokenHash)
            .HasMaxLength(500);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.ExpiresAt)
            .IsRequired();

        builder.Property(s => s.LastActivityAt)
            .IsRequired();

        builder.Property(s => s.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(s => s.UserAgent)
            .HasMaxLength(500);

        builder.Property(s => s.DeviceId)
            .HasMaxLength(100);

        builder.Property(s => s.RevocationReason)
            .HasMaxLength(200);

        builder.HasIndex(s => s.UserId);

        builder.HasIndex(s => s.TokenHash);

        builder.HasIndex(s => new { s.UserId, s.IsActive });

        builder.HasIndex(s => s.ExpiresAt);

        builder.Ignore(s => s.DomainEvents);
    }
}
