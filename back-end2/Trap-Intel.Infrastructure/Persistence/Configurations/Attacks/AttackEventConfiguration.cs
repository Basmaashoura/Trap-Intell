using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Trap_Intel.Domain.Attacks;
using Trap_Intel.Domain.Attacks.ValueObjects;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Attacks;

public class AttackEventConfiguration : IEntityTypeConfiguration<AttackEvent>
{
    public void Configure(EntityTypeBuilder<AttackEvent> builder)
    {
        builder.ToTable("attack_events");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.HoneypotId).HasColumnName("honeypot_id").IsRequired();
        builder.Property(a => a.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(a => a.ThreatActorId).HasColumnName("threat_actor_id");

        builder.Property(a => a.ExternalEventId).HasColumnName("external_event_id").HasMaxLength(100).IsRequired();
        builder.Property(a => a.SensorId).HasColumnName("sensor_id").HasMaxLength(100);
        builder.Property(a => a.SessionId).HasColumnName("session_id");

        // Enums
        builder.Property(a => a.AttackType).HasColumnName("attack_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(a => a.Protocol).HasColumnName("protocol").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(a => a.Severity).HasColumnName("severity").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(a => a.Intent).HasColumnName("intent").HasConversion<string>().HasMaxLength(50);

        // Timestamps
        builder.Property(a => a.Timestamp).HasColumnName("timestamp").IsRequired();
        builder.Property(a => a.ReceivedAt).HasColumnName("received_at").IsRequired();

        // Analysis
        builder.Property(a => a.IsAnalyzed).HasColumnName("is_analyzed").HasDefaultValue(false);
        builder.Property(a => a.ThreatScore).HasColumnName("threat_score").HasPrecision(5, 2).HasDefaultValue(0);
        builder.Property(a => a.IsAnomaly).HasColumnName("is_anomaly").HasDefaultValue(false);

        // Filtering
        builder.Property(a => a.WasEdgeFiltered).HasColumnName("was_edge_filtered").HasDefaultValue(false);
        builder.Property(a => a.FilterReason).HasColumnName("filter_reason").HasMaxLength(500);

        // Captured data
        builder.Property(a => a.Command).HasColumnName("command").HasMaxLength(2000);
        builder.Property(a => a.FileHash).HasColumnName("file_hash").HasMaxLength(64);
        builder.Property(a => a.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(a => a.Payload).HasColumnName("payload").HasColumnType("bytea");

        // Raw JSON
        builder.Property(a => a.RawDataJson).HasColumnName("raw_data").HasColumnType("jsonb").HasDefaultValue("{}");

        // SourceEndpoint (owned)
        builder.OwnsOne(a => a.SourceEndpoint, endpoint =>
        {
            endpoint.Property(e => e.IPAddress).HasColumnName("source_ip").HasMaxLength(45).IsRequired();
            endpoint.Property(e => e.Port).HasColumnName("source_port").IsRequired();
        });

        // TargetEndpoint (owned)
        builder.OwnsOne(a => a.TargetEndpoint, endpoint =>
        {
            endpoint.Property(e => e.IPAddress).HasColumnName("target_ip").HasMaxLength(45).IsRequired();
            endpoint.Property(e => e.Port).HasColumnName("target_port").IsRequired();
        });

        // Geolocation (owned)
        builder.OwnsOne(a => a.Geolocation, geo =>
        {
            geo.Property(g => g.Country).HasColumnName("geo_country").HasMaxLength(100);
            geo.Property(g => g.CountryCode).HasColumnName("geo_country_code").HasMaxLength(2);
            geo.Property(g => g.City).HasColumnName("geo_city").HasMaxLength(100);
            geo.Property(g => g.Region).HasColumnName("geo_region").HasMaxLength(100);
            geo.Property(g => g.Latitude).HasColumnName("geo_latitude").HasPrecision(10, 7);
            geo.Property(g => g.Longitude).HasColumnName("geo_longitude").HasPrecision(10, 7);
            geo.Property(g => g.ISP).HasColumnName("geo_isp").HasMaxLength(255);
            geo.Property(g => g.ASN).HasColumnName("geo_asn").HasMaxLength(50);
        });

        // Credentials (owned, nullable)
        builder.OwnsOne(a => a.Credentials, creds =>
        {
            creds.Property(c => c.Username).HasColumnName("cred_username").HasMaxLength(255);
            creds.Property(c => c.Password).HasColumnName("cred_password").HasMaxLength(255);
            creds.Property(c => c.PasswordHash).HasColumnName("cred_password_hash").HasMaxLength(128);
        });

        // Headers as JSONB
        builder.Property(a => a.Headers)
            .HasColumnName("headers")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new(),
                new ValueComparer<IReadOnlyDictionary<string, string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)));

        // MITRE Techniques as JSONB
        builder.Property(a => a.MitreTechniques)
            .HasColumnName("mitre_techniques")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v.Select(t => new { t.TechniqueId, t.TechniqueName, t.TacticName }), (JsonSerializerOptions?)null),
                v => DeserializeMitreTechniques(v),
                new ValueComparer<IReadOnlyList<MitreTechnique>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        // Ignore computed
        builder.Ignore(a => a.HasRawData);

        // Indexes
        builder.HasIndex(a => a.HoneypotId).HasDatabaseName("ix_attack_events_honeypot_id");
        builder.HasIndex(a => a.OrganizationId).HasDatabaseName("ix_attack_events_organization_id");
        builder.HasIndex(a => a.ExternalEventId).IsUnique().HasDatabaseName("ix_attack_events_external_id_unique");
        builder.HasIndex(a => a.Timestamp).HasDatabaseName("ix_attack_events_timestamp");
        builder.HasIndex(a => a.Severity).HasDatabaseName("ix_attack_events_severity");
        builder.HasIndex(a => a.AttackType).HasDatabaseName("ix_attack_events_attack_type");
        builder.HasIndex(a => a.ThreatActorId).HasDatabaseName("ix_attack_events_threat_actor_id");
        builder.HasIndex(a => new { a.OrganizationId, a.Timestamp }).HasDatabaseName("ix_attack_events_org_timestamp");
        builder.HasIndex(a => new { a.HoneypotId, a.Timestamp }).HasDatabaseName("ix_attack_events_honeypot_timestamp");
    }

    private static IReadOnlyList<MitreTechnique> DeserializeMitreTechniques(string json)
    {
        try
        {
            var elements = JsonSerializer.Deserialize<List<JsonElement>>(json);
            if (elements == null) return new List<MitreTechnique>();

            var result = new List<MitreTechnique>();
            foreach (var e in elements)
            {
                var id = e.GetProperty("TechniqueId").GetString() ?? "";
                var name = e.GetProperty("TechniqueName").GetString() ?? "";
                var tactic = e.TryGetProperty("TacticName", out var t) ? t.GetString() : null;

                var technique = MitreTechnique.Create(id, name, tactic ?? "Unknown");
                if (technique.IsSuccess)
                    result.Add(technique.Value);
            }
            return result;
        }
        catch { return new List<MitreTechnique>(); }
    }
}
