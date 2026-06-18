using Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Accounts;

internal sealed class AccountContactConfiguration : IEntityTypeConfiguration<AccountContact>
{
    public void Configure(EntityTypeBuilder<AccountContact> builder)
    {
        builder.ToTable("account_contacts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.AccountId)
            .IsRequired();

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.IsPrimaryContact)
            .IsRequired();

        builder.Property(c => c.Role)
            .HasMaxLength(100);

        builder.Property(c => c.IsDecisionMaker)
            .IsRequired();

        builder.Property(c => c.IsInviteAccepted)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Configure owned entity for ContactPermissions
        builder.OwnsOne(c => c.Permissions, permissions =>
        {
            permissions.Property(p => p.CanCreateTickets).HasColumnName("can_create_tickets");
            permissions.Property(p => p.CanViewAllTickets).HasColumnName("can_view_all_tickets");
            permissions.Property(p => p.CanViewStressData).HasColumnName("can_view_stress_data");
            permissions.Property(p => p.CanViewReports).HasColumnName("can_view_reports");
            permissions.Property(p => p.CanViewAnalytics).HasColumnName("can_view_analytics");
            permissions.Property(p => p.CanExportData).HasColumnName("can_export_data");
            permissions.Property(p => p.CanManageContacts).HasColumnName("can_manage_contacts");
            permissions.Property(p => p.CanManageSuggestions).HasColumnName("can_manage_suggestions");
            permissions.Property(p => p.CanDownloadFiles).HasColumnName("can_download_files");
            permissions.Property(p => p.ReceiveNotifications).HasColumnName("receive_notifications");
        });

        builder.HasIndex(c => new { c.AccountId, c.UserId })
            .IsUnique();

        builder.HasIndex(c => c.AccountId);

        builder.HasIndex(c => c.UserId);

        builder.HasIndex(c => new { c.AccountId, c.IsPrimaryContact });

        builder.HasIndex(c => c.IsActive);

        builder.Ignore(c => c.DomainEvents);
    }
}
