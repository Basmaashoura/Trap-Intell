using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Alerts.Entities;
using Trap_Intel.Domain.Alerts.Enums;
using Trap_Intel.Domain.Alerts.ValueObjects;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Alerts;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("alerts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.OrganizationId).HasColumnName("organization_id").IsRequired();

        builder.Property(a => a.AlertType).HasColumnName("alert_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(a => a.Severity).HasColumnName("severity").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(a => a.Priority).HasColumnName("priority").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(a => a.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(a => a.EscalationLevel).HasColumnName("escalation_level").HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.Property(a => a.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(a => a.Description).HasColumnName("description").HasMaxLength(5000).IsRequired();
        builder.Property(a => a.Resolution).HasColumnName("resolution").HasMaxLength(5000);

        builder.Property(a => a.AssignedToUserId).HasColumnName("assigned_to_user_id");
        builder.Property(a => a.AcknowledgedByUserId).HasColumnName("acknowledged_by_user_id");
        builder.Property(a => a.AcknowledgedAt).HasColumnName("acknowledged_at");
        builder.Property(a => a.ResolvedByUserId).HasColumnName("resolved_by_user_id");
        builder.Property(a => a.ResolvedAt).HasColumnName("resolved_at");

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(a => a.ExpiresAt).HasColumnName("expires_at");

        // AlertSource (owned)
        builder.OwnsOne(a => a.Source, source =>
        {
            source.Property(s => s.SourceType).HasColumnName("source_type").HasMaxLength(50).IsRequired();
            source.Property(s => s.SourceId).HasColumnName("source_id");
            source.Property(s => s.SourceName).HasColumnName("source_name").HasMaxLength(255);
            source.Property(s => s.IPAddress).HasColumnName("source_ip").HasMaxLength(45);
        });

        // SnoozeInfo (owned, nullable)
        builder.OwnsOne(a => a.SnoozeInfo, snooze =>
        {
            snooze.Property(s => s.SnoozedAt).HasColumnName("snoozed_at");
            snooze.Property(s => s.SnoozeUntil).HasColumnName("snooze_until");
            snooze.Property(s => s.SnoozedByUserId).HasColumnName("snoozed_by_user_id");
            snooze.Property(s => s.Reason).HasColumnName("snooze_reason").HasMaxLength(500);
            snooze.Ignore(s => s.IsExpired);
        });

        // Child entities
        builder.HasMany(a => a.Actions)
            .WithOne()
            .HasForeignKey(action => action.AlertId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Comments)
            .WithOne()
            .HasForeignKey(comment => comment.AlertId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Notifications)
            .WithOne()
            .HasForeignKey(n => n.AlertId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Escalations)
            .WithOne()
            .HasForeignKey(e => e.AlertId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(a => a.OrganizationId).HasDatabaseName("ix_alerts_organization_id");
        builder.HasIndex(a => a.Status).HasDatabaseName("ix_alerts_status");
        builder.HasIndex(a => a.Severity).HasDatabaseName("ix_alerts_severity");
        builder.HasIndex(a => a.AlertType).HasDatabaseName("ix_alerts_alert_type");
        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("ix_alerts_created_at");
        builder.HasIndex(a => a.AssignedToUserId).HasDatabaseName("ix_alerts_assigned_to");
        builder.HasIndex(a => new { a.OrganizationId, a.Status }).HasDatabaseName("ix_alerts_org_status");
        builder.HasIndex(a => new { a.OrganizationId, a.Severity, a.Status }).HasDatabaseName("ix_alerts_org_severity_status");
    }
}

public class AlertActionEntityConfiguration : IEntityTypeConfiguration<AlertActionEntity>
{
    public void Configure(EntityTypeBuilder<AlertActionEntity> builder)
    {
        builder.ToTable("alert_actions");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(a => a.AlertId).HasColumnName("alert_id").IsRequired();

        builder.Property(a => a.ActionType)
            .HasColumnName("action_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.PerformedByUserId).HasColumnName("performed_by_user_id").IsRequired();
        builder.Property(a => a.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(a => a.PerformedAt).HasColumnName("performed_at").IsRequired();
        builder.Property(a => a.Metadata).HasColumnName("metadata").HasColumnType("jsonb");

        builder.HasIndex(a => a.AlertId).HasDatabaseName("ix_alert_actions_alert_id");
        builder.HasIndex(a => a.PerformedAt).HasDatabaseName("ix_alert_actions_performed_at");
    }
}

public class AlertCommentEntityConfiguration : IEntityTypeConfiguration<AlertCommentEntity>
{
    public void Configure(EntityTypeBuilder<AlertCommentEntity> builder)
    {
        builder.ToTable("alert_comments");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.AlertId).HasColumnName("alert_id").IsRequired();

        builder.Property(c => c.Content).HasColumnName("content").HasMaxLength(10000).IsRequired();
        builder.Property(c => c.AuthorUserId).HasColumnName("author_user_id").IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.EditedAt).HasColumnName("edited_at");
        builder.Property(c => c.EditedByUserId).HasColumnName("edited_by_user_id");
        builder.Property(c => c.IsEdited).HasColumnName("is_edited").HasDefaultValue(false);
        builder.Property(c => c.IsInternal).HasColumnName("is_internal").HasDefaultValue(false);
        builder.Property(c => c.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");
        builder.Property(c => c.DeletedByUserId).HasColumnName("deleted_by_user_id");
        builder.Property(c => c.ParentCommentId).HasColumnName("parent_comment_id");

        builder.HasIndex(c => c.AlertId).HasDatabaseName("ix_alert_comments_alert_id");
        builder.HasIndex(c => c.ParentCommentId).HasDatabaseName("ix_alert_comments_parent_id");
    }
}

public class AlertNotificationEntityConfiguration : IEntityTypeConfiguration<AlertNotificationEntity>
{
    public void Configure(EntityTypeBuilder<AlertNotificationEntity> builder)
    {
        builder.ToTable("alert_notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(n => n.AlertId).HasColumnName("alert_id").IsRequired();

        builder.Property(n => n.Channel)
            .HasColumnName("channel")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(n => n.Trigger)
            .HasColumnName("trigger")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(n => n.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(n => n.Recipients)
            .HasColumnName("recipients")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>(),
                new ValueComparer<IReadOnlyList<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        builder.Property(n => n.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(n => n.SentAt).HasColumnName("sent_at");
        builder.Property(n => n.DeliveredAt).HasColumnName("delivered_at");
        builder.Property(n => n.FailedAt).HasColumnName("failed_at");
        builder.Property(n => n.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
        builder.Property(n => n.MaxRetries).HasColumnName("max_retries").HasDefaultValue(3);
        builder.Property(n => n.FailureReason).HasColumnName("failure_reason").HasMaxLength(1000);
        builder.Property(n => n.ExternalMessageId).HasColumnName("external_message_id").HasMaxLength(255);
        builder.Property(n => n.ProviderResponse).HasColumnName("provider_response").HasMaxLength(2000);
        builder.Property(n => n.Subject).HasColumnName("subject").HasMaxLength(500);
        builder.Property(n => n.BodyPreview).HasColumnName("body_preview").HasMaxLength(500);

        builder.HasIndex(n => n.AlertId).HasDatabaseName("ix_alert_notifications_alert_id");
        builder.HasIndex(n => n.Status).HasDatabaseName("ix_alert_notifications_status");
    }
}

public class AlertEscalationEntityConfiguration : IEntityTypeConfiguration<AlertEscalationEntity>
{
    public void Configure(EntityTypeBuilder<AlertEscalationEntity> builder)
    {
        builder.ToTable("alert_escalations");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.AlertId).HasColumnName("alert_id").IsRequired();

        builder.Property(e => e.FromLevel)
            .HasColumnName("from_level")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ToLevel)
            .HasColumnName("to_level")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(2000).IsRequired();
        builder.Property(e => e.EscalatedByUserId).HasColumnName("escalated_by_user_id");
        builder.Property(e => e.IsAutomatic).HasColumnName("is_automatic").HasDefaultValue(false);
        builder.Property(e => e.EscalatedAt).HasColumnName("escalated_at").IsRequired();
        builder.Property(e => e.SLABreached).HasColumnName("sla_breached").HasMaxLength(255);
        builder.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb");

        // TimeToEscalate stored as ticks
        builder.Property(e => e.TimeToEscalate)
            .HasColumnName("time_to_escalate_ticks")
            .HasConversion(
                v => v.HasValue ? v.Value.Ticks : (long?)null,
                v => v.HasValue ? TimeSpan.FromTicks(v.Value) : null);

        // NotifiedUserIds as JSONB
        builder.Property(e => e.NotifiedUserIds)
            .HasColumnName("notified_user_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>(),
                new ValueComparer<IReadOnlyList<Guid>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        builder.HasIndex(e => e.AlertId).HasDatabaseName("ix_alert_escalations_alert_id");
        builder.HasIndex(e => e.EscalatedAt).HasDatabaseName("ix_alert_escalations_escalated_at");
    }
}
