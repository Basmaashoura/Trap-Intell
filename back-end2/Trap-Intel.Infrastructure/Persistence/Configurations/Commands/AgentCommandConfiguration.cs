using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Trap_Intel.Domain.Commands;
using Trap_Intel.Domain.Commands.Enums;
using Trap_Intel.Domain.Commands.ValueObjects;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Commands;

public class AgentCommandConfiguration : IEntityTypeConfiguration<AgentCommand>
{
    public void Configure(EntityTypeBuilder<AgentCommand> builder)
    {
        builder.ToTable("agent_commands");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.HoneypotId).HasColumnName("honeypot_id").IsRequired();
        builder.Property(c => c.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(c => c.IssuedByUserId).HasColumnName("issued_by_user_id").IsRequired();

        // Enums
        builder.Property(c => c.CommandType)
            .HasColumnName("command_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(CommandPriority.Normal);

        builder.Property(c => c.DeliveryMethod)
            .HasColumnName("delivery_method")
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(CommandDeliveryMethod.Immediate);

        // Payload (owned value object, stored as JSONB)
        builder.OwnsOne(c => c.Payload, payload =>
        {
            payload.Property(p => p.JsonPayload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .IsRequired();
        });

        // Timeout (owned value object)
        builder.OwnsOne(c => c.Timeout, timeout =>
        {
            timeout.Property(t => t.Timeout)
                .HasColumnName("timeout_seconds")
                .HasConversion(
                    v => (int)v.TotalSeconds,
                    v => TimeSpan.FromSeconds(v));

            timeout.Property(t => t.MaxRetries)
                .HasColumnName("max_retries")
                .HasDefaultValue(3);
        });

        // ExecutionResult (owned, nullable)
        builder.OwnsOne(c => c.ExecutionResult, result =>
        {
            result.Property(r => r.Success)
                .HasColumnName("result_success");

            result.Property(r => r.Message)
                .HasColumnName("result_message")
                .HasMaxLength(2000);

            result.Property(r => r.ResultData)
                .HasColumnName("result_data")
                .HasColumnType("jsonb");

            result.Property(r => r.CompletedAt)
                .HasColumnName("result_completed_at");

            result.Property(r => r.ExecutionDuration)
                .HasColumnName("result_duration_ms")
                .HasConversion(
                    v => v.HasValue ? (long?)v.Value.TotalMilliseconds : null,
                    v => v.HasValue ? TimeSpan.FromMilliseconds((double)v.Value) : null);
        });

        // Simple properties
        builder.Property(c => c.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        builder.Property(c => c.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);

        // Timestamps
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.SentAt).HasColumnName("sent_at");
        builder.Property(c => c.AcknowledgedAt).HasColumnName("acknowledged_at");
        builder.Property(c => c.ExecutionStartedAt).HasColumnName("execution_started_at");
        builder.Property(c => c.CompletedAt).HasColumnName("completed_at");
        builder.Property(c => c.ScheduledFor).HasColumnName("scheduled_for");
        builder.Property(c => c.TimeoutAt).HasColumnName("timeout_at");

        // Indexes
        builder.HasIndex(c => c.HoneypotId).HasDatabaseName("ix_agent_commands_honeypot_id");
        builder.HasIndex(c => c.OrganizationId).HasDatabaseName("ix_agent_commands_organization_id");
        builder.HasIndex(c => c.Status).HasDatabaseName("ix_agent_commands_status");
        builder.HasIndex(c => c.CommandType).HasDatabaseName("ix_agent_commands_command_type");
        builder.HasIndex(c => c.Priority).HasDatabaseName("ix_agent_commands_priority");
        builder.HasIndex(c => c.CreatedAt).HasDatabaseName("ix_agent_commands_created_at");
        builder.HasIndex(c => new { c.HoneypotId, c.Status }).HasDatabaseName("ix_agent_commands_honeypot_status");
        builder.HasIndex(c => new { c.OrganizationId, c.Status }).HasDatabaseName("ix_agent_commands_org_status");
        builder.HasIndex(c => new { c.Status, c.Priority, c.CreatedAt }).HasDatabaseName("ix_agent_commands_queue");
    }
}
