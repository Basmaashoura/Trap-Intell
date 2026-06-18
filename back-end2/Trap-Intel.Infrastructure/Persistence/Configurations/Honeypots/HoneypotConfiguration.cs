using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Trap_Intel.Domain.Honeypots;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Honeypots;

public class HoneypotConfiguration : IEntityTypeConfiguration<Honeypot>
{
    public void Configure(EntityTypeBuilder<Honeypot> builder)
    {
        builder.ToTable("honeypots");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(h => h.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(h => h.SubscriptionId)
            .HasColumnName("subscription_id")
            .IsRequired();

        builder.Property(h => h.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        // Enums stored as strings
        builder.Property(h => h.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(h => h.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(h => h.DeploymentLocation)
            .HasColumnName("deployment_location")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(h => h.HeartbeatStatus)
            .HasColumnName("heartbeat_status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(HeartbeatStatus.Unknown);

        // Timestamps
        builder.Property(h => h.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(h => h.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(h => h.LastHeartbeat).HasColumnName("last_heartbeat");
        builder.Property(h => h.LastHeartbeatAt).HasColumnName("last_heartbeat_at");
        builder.Property(h => h.LastLogFetch).HasColumnName("last_log_fetch");
        builder.Property(h => h.DeployedAt).HasColumnName("deployed_at");
        builder.Property(h => h.TerminatedAt).HasColumnName("terminated_at");

        // Agent properties
        builder.Property(h => h.ConsecutiveMissedHeartbeats)
            .HasColumnName("consecutive_missed_heartbeats")
            .HasDefaultValue(0);
        builder.Property(h => h.IsConnected).HasColumnName("is_connected").HasDefaultValue(false);
        builder.Property(h => h.AgentId).HasColumnName("agent_id").HasMaxLength(100);
        builder.Property(h => h.AgentVersion).HasColumnName("agent_version").HasMaxLength(50);

        // Configuration (owned)
        builder.OwnsOne(h => h.Configuration, config =>
        {
            config.Property(c => c.Port).HasColumnName("config_port").IsRequired();
            config.Property(c => c.Credentials).HasColumnName("config_credentials").HasMaxLength(500);
            config.Property(c => c.CaptureLevel)
                .HasColumnName("config_capture_level")
                .HasConversion<string>()
                .HasMaxLength(50);
            config.Property(c => c.MaxConnections).HasColumnName("config_max_connections");
            config.Property(c => c.RecordPayload).HasColumnName("config_record_payload").HasDefaultValue(true);
            config.Property(c => c.RetentionDays).HasColumnName("config_retention_days").HasDefaultValue(90);
            config.Property(c => c.CustomSettings)
                .HasColumnName("config_custom_settings")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new());
        });

        // ExternalService (owned, nullable)
        builder.OwnsOne(h => h.ExternalService, ext =>
        {
            ext.Property(e => e.ServiceId).HasColumnName("external_service_id").HasMaxLength(100);
            ext.Property(e => e.ServiceName).HasColumnName("external_service_name").HasMaxLength(100);
            ext.Property(e => e.ApiEndpoint).HasColumnName("external_api_endpoint").HasMaxLength(500);
            ext.Property(e => e.LinkedAt).HasColumnName("external_linked_at");
            ext.Property(e => e.ServiceVersion).HasColumnName("external_service_version").HasMaxLength(50);
        });

        // NetworkInfo (owned, nullable)
        builder.OwnsOne(h => h.NetworkInfo, net =>
        {
            net.Property(n => n.IpAddress).HasColumnName("network_ip_address").HasMaxLength(45);
            net.Property(n => n.Port).HasColumnName("network_port");
            net.Property(n => n.Hostname).HasColumnName("network_hostname").HasMaxLength(255);
            net.Property(n => n.MacAddress).HasColumnName("network_mac_address").HasMaxLength(17);
            net.Property(n => n.NetworkInterface).HasColumnName("network_interface").HasMaxLength(100);
        });

        // Health (owned)
        builder.OwnsOne(h => h.Health, health =>
        {
            health.Property(hp => hp.Status)
                .HasColumnName("health_status")
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(HoneypotHealthStatus.Unknown);
            health.Property(hp => hp.LastHeartbeat).HasColumnName("health_last_heartbeat");
            health.Property(hp => hp.CpuUsagePercent).HasColumnName("health_cpu_percent").HasPrecision(5, 2);
            health.Property(hp => hp.MemoryUsagePercent).HasColumnName("health_memory_percent").HasPrecision(5, 2);
            health.Property(hp => hp.DiskUsagePercent).HasColumnName("health_disk_percent").HasPrecision(5, 2);
            health.Property(hp => hp.ActiveConnections).HasColumnName("health_active_connections");
            health.Property(hp => hp.StorageUsedBytes).HasColumnName("health_storage_used_bytes");
            health.Property(hp => hp.FailedConnectionAttempts).HasColumnName("health_failed_connections");
        });

        // Statistics (owned)
        builder.OwnsOne(h => h.Statistics, stats =>
        {
            stats.Property(s => s.TotalEventsCapture).HasColumnName("stats_total_events");
            stats.Property(s => s.CriticalEvents).HasColumnName("stats_critical_events");
            stats.Property(s => s.HighSeverityEvents).HasColumnName("stats_high_events");
            stats.Property(s => s.MediumSeverityEvents).HasColumnName("stats_medium_events");
            stats.Property(s => s.LowSeverityEvents).HasColumnName("stats_low_events");
            stats.Property(s => s.UniqueSourceIps).HasColumnName("stats_unique_ips");
            stats.Property(s => s.FailedAuthenticationAttempts).HasColumnName("stats_failed_auth");
            stats.Property(s => s.SuccessfulConnectionAttempts).HasColumnName("stats_successful_connections");
            stats.Property(s => s.FirstEventTime).HasColumnName("stats_first_event_time");
            stats.Property(s => s.LastEventTime).HasColumnName("stats_last_event_time");
        });

        // Notes as JSONB
        builder.Property(h => h.Notes)
            .HasColumnName("notes")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new(),
                new ValueComparer<IReadOnlyList<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        // Indexes
        builder.HasIndex(h => h.OrganizationId).HasDatabaseName("ix_honeypots_organization_id");
        builder.HasIndex(h => h.SubscriptionId).HasDatabaseName("ix_honeypots_subscription_id");
        builder.HasIndex(h => h.Status).HasDatabaseName("ix_honeypots_status");
        builder.HasIndex(h => h.Type).HasDatabaseName("ix_honeypots_type");
        builder.HasIndex(h => new { h.OrganizationId, h.Status }).HasDatabaseName("ix_honeypots_org_status");
    }
}
