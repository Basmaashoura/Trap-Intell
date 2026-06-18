using Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Accounts;

internal sealed class AccountSettingsConfiguration : IEntityTypeConfiguration<AccountSettings>
{
    public void Configure(EntityTypeBuilder<AccountSettings> builder)
    {
        builder.ToTable("account_settings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.AccountId)
            .IsRequired();

        builder.Property(s => s.MaxEmployees)
            .IsRequired()
            .HasDefaultValue(5);

        builder.Property(s => s.MaxMonitoredUsers)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.MaxStorageMb)
            .IsRequired()
            .HasDefaultValue(1024);

        builder.Property(s => s.DashboardAccessEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.AllowEmployeeSelfInvite)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.RequireInviteApproval)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.InviteExpirationDays)
            .IsRequired()
            .HasDefaultValue(7);

        builder.HasIndex(s => s.AccountId)
            .IsUnique();

        builder.Ignore(s => s.DomainEvents);
    }
}
