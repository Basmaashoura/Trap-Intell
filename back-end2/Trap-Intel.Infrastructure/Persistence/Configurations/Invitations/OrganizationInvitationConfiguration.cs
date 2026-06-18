using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trap_Intel.Domain.Invitations;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Invitations;

public sealed class OrganizationInvitationConfiguration : IEntityTypeConfiguration<OrganizationInvitation>
{
	public void Configure(EntityTypeBuilder<OrganizationInvitation> builder)
	{
		builder.ToTable("organization_invitations");

		builder.HasKey(i => i.Id);

		builder.Property(i => i.Id)
			.HasColumnName("id")
			.ValueGeneratedNever();

		builder.Property(i => i.OrganizationId)
			.HasColumnName("organization_id")
			.IsRequired();

		builder.Property(i => i.Email)
			.HasColumnName("email")
			.HasMaxLength(255)
			.IsRequired();

		builder.Property(i => i.RoleId)
			.HasColumnName("role_id")
			.IsRequired();

		builder.Property(i => i.PersonalMessage)
			.HasColumnName("personal_message")
			.HasMaxLength(1000);

		builder.Property(i => i.InvitedByUserId)
			.HasColumnName("invited_by_user_id")
			.IsRequired();

		builder.Property(i => i.TokenHash)
			.HasColumnName("token_hash")
			.HasMaxLength(128)
			.IsRequired();

		builder.Property(i => i.Status)
			.HasColumnName("status")
			.HasConversion<string>()
			.HasMaxLength(50)
			.IsRequired();

		builder.Property(i => i.ExpiresAt)
			.HasColumnName("expires_at")
			.IsRequired();

		builder.Property(i => i.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired();

		builder.Property(i => i.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired();

		builder.Property(i => i.AcceptedAt)
			.HasColumnName("accepted_at");

		builder.Property(i => i.AcceptedByUserId)
			.HasColumnName("accepted_by_user_id");

		builder.Property(i => i.DeclinedAt)
			.HasColumnName("declined_at");

		builder.Property(i => i.DeclineReason)
			.HasColumnName("decline_reason")
			.HasMaxLength(500);

		builder.Property(i => i.RevokedAt)
			.HasColumnName("revoked_at");

		builder.Property(i => i.RevokedByUserId)
			.HasColumnName("revoked_by_user_id");

		builder.Property(i => i.RevocationReason)
			.HasColumnName("revocation_reason")
			.HasMaxLength(500);

		builder.Property(i => i.RemindersSent)
			.HasColumnName("reminders_sent")
			.HasDefaultValue(0)
			.IsRequired();

		builder.Property(i => i.LastReminderSentAt)
			.HasColumnName("last_reminder_sent_at");

		builder.Property(i => i.AcceptedFromIP)
			.HasColumnName("accepted_from_ip")
			.HasMaxLength(45);

		builder.HasOne<Role>()
			.WithMany()
			.HasForeignKey(i => i.RoleId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasIndex(i => i.OrganizationId)
			.HasDatabaseName("ix_organization_invitations_organization_id");

		builder.HasIndex(i => i.Email)
			.HasDatabaseName("ix_organization_invitations_email");

		builder.HasIndex(i => i.Status)
			.HasDatabaseName("ix_organization_invitations_status");

		builder.HasIndex(i => i.RoleId)
			.HasDatabaseName("ix_organization_invitations_role_id");

		builder.HasIndex(i => i.TokenHash)
			.HasDatabaseName("ix_organization_invitations_token_hash");

		builder.HasIndex(i => i.ExpiresAt)
			.HasDatabaseName("ix_organization_invitations_expires_at");
	}
}
