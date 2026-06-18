using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Auditing;

public class AuditTrailConfiguration : IEntityTypeConfiguration<AuditTrail>
{
    public void Configure(EntityTypeBuilder<AuditTrail> builder)
    {
        builder.ToTable("audit_trails");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(a => a.UserId).HasColumnName("user_id");
        builder.Property(a => a.ResourceId).HasColumnName("resource_id").IsRequired();

        builder.Property(a => a.ResourceType)
            .HasColumnName("resource_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.Action)
            .HasColumnName("action")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.Severity)
            .HasColumnName("severity")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.Reason).HasColumnName("reason").HasMaxLength(2000);
        builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(a => a.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(a => a.Timestamp).HasColumnName("timestamp").IsRequired();
        builder.Property(a => a.RetentionPeriodDays).HasColumnName("retention_period_days").HasDefaultValue(365);

        builder.Property(a => a.IsArchived).HasColumnName("is_archived").HasDefaultValue(false);
        builder.Property(a => a.IsAcknowledged).HasColumnName("is_acknowledged").HasDefaultValue(false);
        builder.Property(a => a.AcknowledgedBy).HasColumnName("acknowledged_by");
        builder.Property(a => a.AcknowledgedAt).HasColumnName("acknowledged_at");
        builder.Property(a => a.AcknowledgeNotes).HasColumnName("acknowledge_notes").HasMaxLength(1000);
        builder.Property(a => a.RecordHash).HasColumnName("record_hash").HasMaxLength(128);

        // Changes as JSONB
        builder.Property(a => a.Changes)
            .HasColumnName("changes")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v.Select(c => new { c.PropertyName, c.OldValue, c.NewValue }), (JsonSerializerOptions?)null),
                v => DeserializeChanges(v),
                new ValueComparer<IReadOnlyList<AuditChange>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        // Compliance Standards as JSONB array of strings
        builder.Property(a => a.ComplianceStandards)
            .HasColumnName("compliance_standards")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v.Select(s => s.ToString()), (JsonSerializerOptions?)null),
                v => DeserializeStandards(v),
                new ValueComparer<IReadOnlyList<ComplianceStandard>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        // Ignore computed properties
        builder.Ignore(a => a.ExpirationDate);
        builder.Ignore(a => a.IsExpired);

        // Indexes
        builder.HasIndex(a => a.OrganizationId).HasDatabaseName("ix_audit_trails_organization_id");
        builder.HasIndex(a => a.UserId).HasDatabaseName("ix_audit_trails_user_id");
        builder.HasIndex(a => a.ResourceType).HasDatabaseName("ix_audit_trails_resource_type");
        builder.HasIndex(a => a.ResourceId).HasDatabaseName("ix_audit_trails_resource_id");
        builder.HasIndex(a => a.Action).HasDatabaseName("ix_audit_trails_action");
        builder.HasIndex(a => a.Timestamp).HasDatabaseName("ix_audit_trails_timestamp");
        builder.HasIndex(a => a.Severity).HasDatabaseName("ix_audit_trails_severity");
        builder.HasIndex(a => new { a.OrganizationId, a.Timestamp }).HasDatabaseName("ix_audit_trails_org_timestamp");
        builder.HasIndex(a => new { a.OrganizationId, a.ResourceType, a.ResourceId }).HasDatabaseName("ix_audit_trails_org_resource");
    }

    private static IReadOnlyList<AuditChange> DeserializeChanges(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<AuditChange>();
        try
        {
            var elements = JsonSerializer.Deserialize<List<JsonElement>>(json, (JsonSerializerOptions?)null);
            if (elements == null) return new List<AuditChange>();

            var result = new List<AuditChange>();
            foreach (var e in elements)
            {
                var propName = e.GetProperty("propertyName").GetString() ?? e.GetProperty("PropertyName").GetString() ?? "";
                var oldVal = e.TryGetProperty("oldValue", out var o1) && o1.ValueKind != JsonValueKind.Null ? o1.GetString() : 
                               (e.TryGetProperty("OldValue", out var o2) && o2.ValueKind != JsonValueKind.Null ? o2.GetString() : null);

                var newVal = e.TryGetProperty("newValue", out var n1) && n1.ValueKind != JsonValueKind.Null ? n1.GetString() : 
                               (e.TryGetProperty("NewValue", out var n2) && n2.ValueKind != JsonValueKind.Null ? n2.GetString() : null);

                var changeResult = AuditChange.Create(propName, oldVal, newVal);
                if (changeResult.IsSuccess)
                    result.Add(changeResult.Value);
            }
            return result;
        }
        catch { return new List<AuditChange>(); }
    }

    private static IReadOnlyList<ComplianceStandard> DeserializeStandards(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<ComplianceStandard>();
        try
        {
            var strings = JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null);
            if (strings == null) return new List<ComplianceStandard>();

            return strings.Select(s => Enum.TryParse<ComplianceStandard>(s, out var std) ? std : (ComplianceStandard?)null)
                          .Where(s => s.HasValue)
                          .Select(s => s!.Value)
                          .ToList();
        }
        catch { return new List<ComplianceStandard>(); }
    }
}
