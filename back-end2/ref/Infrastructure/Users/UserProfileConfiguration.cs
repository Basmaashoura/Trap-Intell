using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Users;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.Department)
            .IsRequired();

        builder.Property(p => p.DisplayJobTitle)
            .HasMaxLength(100);

        builder.Property(p => p.InternalJobTitle)
            .HasMaxLength(100);

        builder.Property(p => p.HourlyCost)
            .HasPrecision(18, 2);

        builder.Property(p => p.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(p => p.AvatarUrl)
            .HasMaxLength(500)
            .HasConversion(
                v => v != null ? v.ToString() : null,
                v => v != null ? new Uri(v) : null);

        builder.Property(p => p.Bio)
            .HasMaxLength(1000);

        builder.Property(p => p.Skills)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());

        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.HasIndex(p => p.Department);

        builder.Ignore(p => p.DomainEvents);
    }
}
