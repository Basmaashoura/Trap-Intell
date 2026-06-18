using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Trap_Intel.Domain.ApiKeys;
using Trap_Intel.Domain.ApiKeys.Enums;
using Trap_Intel.Domain.ApiKeys.ValueObjects;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.ApiKeys;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(a => a.Description).HasColumnName("description").HasMaxLength(500);

        builder.Property(a => a.KeyPrefix).HasColumnName("key_prefix").HasMaxLength(20).IsRequired();
        builder.Property(a => a.KeyHash).HasColumnName("key_hash").HasMaxLength(128).IsRequired();

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.KeyType)
            .HasColumnName("key_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.ExpiresAt).HasColumnName("expires_at");
        builder.Property(a => a.LastUsedAt).HasColumnName("last_used_at");
        builder.Property(a => a.LastUsedFromIP).HasColumnName("last_used_from_ip").HasMaxLength(45);
        builder.Property(a => a.TotalUsageCount).HasColumnName("total_usage_count").HasDefaultValue(0);

        // RateLimit (owned)
        builder.OwnsOne(a => a.RateLimit, rate =>
        {
            rate.Property(r => r.RequestsPerMinute).HasColumnName("rate_limit_per_minute").HasDefaultValue(60);
            rate.Property(r => r.RequestsPerHour).HasColumnName("rate_limit_per_hour").HasDefaultValue(1000);
            rate.Property(r => r.RequestsPerDay).HasColumnName("rate_limit_per_day").HasDefaultValue(10000);
        });

        builder.Property(a => a.CurrentWindowUsage).HasColumnName("current_window_usage").HasDefaultValue(0);
        builder.Property(a => a.RateLimitWindowStart).HasColumnName("rate_limit_window_start");

        // AllowedIPs as JSONB
        builder.Property(a => a.AllowedIPs)
            .HasColumnName("allowed_ips")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

        builder.Property(a => a.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(a => a.RevokedByUserId).HasColumnName("revoked_by_user_id");
        builder.Property(a => a.RevokedAt).HasColumnName("revoked_at");
        builder.Property(a => a.RevocationReason).HasColumnName("revocation_reason").HasMaxLength(500);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(a => a.Version).HasColumnName("version").HasDefaultValue(1);

        // Permissions as JSONB
        builder.Property(a => a.Permissions)
            .HasColumnName("permissions")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v.Select(p => p.ToString()), (JsonSerializerOptions?)null),
                v => DeserializePermissions(v),
                new ValueComparer<IReadOnlyList<ApiKeyPermission>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, p) => HashCode.Combine(a, p.GetHashCode())),
                    c => c.ToList()));

        // RecentUsage as JSONB
        builder.Property(a => a.RecentUsage)
            .HasColumnName("recent_usage")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v.Select(u => new { u.Timestamp, u.IPAddress, u.Endpoint, u.Success, u.ErrorMessage }), (JsonSerializerOptions?)null),
                v => DeserializeUsageRecords(v),
                new ValueComparer<IReadOnlyList<ApiKeyUsageRecord>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, u) => HashCode.Combine(a, u.GetHashCode())),
                    c => c.ToList()));

        // Indexes
        builder.HasIndex(a => a.OrganizationId).HasDatabaseName("ix_api_keys_organization_id");
        builder.HasIndex(a => a.KeyPrefix).HasDatabaseName("ix_api_keys_prefix");
        builder.HasIndex(a => a.KeyHash).IsUnique().HasDatabaseName("ix_api_keys_hash_unique");
        builder.HasIndex(a => a.Status).HasDatabaseName("ix_api_keys_status");
        builder.HasIndex(a => new { a.OrganizationId, a.Status }).HasDatabaseName("ix_api_keys_org_status");
    }

    private static IReadOnlyList<ApiKeyPermission> DeserializePermissions(string json)
    {
        try
        {
            var strings = JsonSerializer.Deserialize<List<string>>(json);
            if (strings == null) return new List<ApiKeyPermission>();

            return strings
                .Where(s => Enum.TryParse<ApiKeyPermission>(s, out _))
                .Select(s => Enum.Parse<ApiKeyPermission>(s))
                .ToList();
        }
        catch { return new List<ApiKeyPermission>(); }
    }

    private static IReadOnlyList<ApiKeyUsageRecord> DeserializeUsageRecords(string json)
    {
        try
        {
            var elements = JsonSerializer.Deserialize<List<JsonElement>>(json);
            if (elements == null) return new List<ApiKeyUsageRecord>();

            return elements.Select(e =>
            {
                var ts = e.GetProperty("Timestamp").GetDateTime();
                var ip = e.GetProperty("IPAddress").GetString() ?? "";
                var ep = e.GetProperty("Endpoint").GetString() ?? "";
                var success = e.GetProperty("Success").GetBoolean();
                var reason = e.TryGetProperty("ErrorMessage", out var r) ? r.GetString() : null;
                return new ApiKeyUsageRecord(ts, ip, ep, success, reason);
            }).ToList();
        }
        catch { return new List<ApiKeyUsageRecord>(); }
    }
}
