using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Trap_Intel.Domain.Webhooks;
using Trap_Intel.Domain.Webhooks.Enums;
using Trap_Intel.Domain.Webhooks.ValueObjects;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Webhooks;

public class WebhookConfiguration : IEntityTypeConfiguration<Webhook>
{
    public void Configure(EntityTypeBuilder<Webhook> builder)
    {
        builder.ToTable("webhooks");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(w => w.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(w => w.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(w => w.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(w => w.Url).HasColumnName("url").HasMaxLength(2000).IsRequired();
        builder.Property(w => w.SecretHash).HasColumnName("secret_hash").HasMaxLength(128).IsRequired();

        builder.Property(w => w.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(w => w.ContentType)
            .HasColumnName("content_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(WebhookContentType.Json);

        builder.Property(w => w.SslVerificationEnabled).HasColumnName("ssl_verification_enabled").HasDefaultValue(true);
        builder.Property(w => w.TimeoutSeconds).HasColumnName("timeout_seconds").HasDefaultValue(30);
        builder.Property(w => w.MaxRetries).HasColumnName("max_retries").HasDefaultValue(3);

        // CustomHeaders as JSONB
        builder.Property(w => w.CustomHeaders)
            .HasColumnName("custom_headers")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new());

        // SubscribedEvents as JSONB (list of enums)
        builder.Property(w => w.SubscribedEvents)
            .HasColumnName("subscribed_events")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v.Select(e => e.ToString()), (JsonSerializerOptions?)null),
                v => DeserializeWebhookEvents(v),
                new ValueComparer<IReadOnlyList<WebhookEventType>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, e) => HashCode.Combine(a, e.GetHashCode())),
                    c => c.ToList()));

        builder.Property(w => w.LastTriggeredAt).HasColumnName("last_triggered_at");
        builder.Property(w => w.LastSuccessAt).HasColumnName("last_success_at");
        builder.Property(w => w.LastFailureAt).HasColumnName("last_failure_at");
        builder.Property(w => w.LastFailureMessage).HasColumnName("last_failure_message").HasMaxLength(2000);
        builder.Property(w => w.ConsecutiveFailures).HasColumnName("consecutive_failures").HasDefaultValue(0);
        builder.Property(w => w.TotalDeliveries).HasColumnName("total_deliveries").HasDefaultValue(0);
        builder.Property(w => w.SuccessfulDeliveries).HasColumnName("successful_deliveries").HasDefaultValue(0);
        builder.Property(w => w.FailedDeliveries).HasColumnName("failed_deliveries").HasDefaultValue(0);
        builder.Property(w => w.VerifiedAt).HasColumnName("verified_at");
        builder.Property(w => w.IsVerified).HasColumnName("is_verified").HasDefaultValue(false);
        builder.Property(w => w.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(w => w.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(w => w.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // RecentDeliveries as JSONB
        builder.Property(w => w.RecentDeliveries)
            .HasColumnName("recent_deliveries")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v.Select(d => new {
                    d.Timestamp,
                    EventType = d.EventType.ToString(),
                    d.Success,
                    d.ResponseStatusCode,
                    DurationMs = d.Duration.TotalMilliseconds,
                    d.ErrorMessage
                }), (JsonSerializerOptions?)null),
                v => DeserializeDeliveryRecords(v),
                new ValueComparer<IReadOnlyList<WebhookDeliveryRecord>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, d) => HashCode.Combine(a, d.GetHashCode())),
                    c => c.ToList()));

        // Indexes
        builder.HasIndex(w => w.OrganizationId).HasDatabaseName("ix_webhooks_organization_id");
        builder.HasIndex(w => w.Status).HasDatabaseName("ix_webhooks_status");
        builder.HasIndex(w => new { w.OrganizationId, w.Status }).HasDatabaseName("ix_webhooks_org_status");
    }

    private static IReadOnlyList<WebhookEventType> DeserializeWebhookEvents(string json)
    {
        try
        {
            var strings = JsonSerializer.Deserialize<List<string>>(json);
            if (strings == null) return new List<WebhookEventType>();

            return strings
                .Where(s => Enum.TryParse<WebhookEventType>(s, out _))
                .Select(s => Enum.Parse<WebhookEventType>(s))
                .ToList();
        }
        catch { return new List<WebhookEventType>(); }
    }

    private static IReadOnlyList<WebhookDeliveryRecord> DeserializeDeliveryRecords(string json)
    {
        try
        {
            var elements = JsonSerializer.Deserialize<List<JsonElement>>(json);
            if (elements == null) return new List<WebhookDeliveryRecord>();

            return elements.Select(e =>
            {
                var ts = e.GetProperty("Timestamp").GetDateTime();
                var eventTypeStr = e.GetProperty("EventType").GetString() ?? "";
                var eventType = Enum.TryParse<WebhookEventType>(eventTypeStr, out var et) ? et : WebhookEventType.None;
                var success = e.GetProperty("Success").GetBoolean();
                var statusCode = e.TryGetProperty("ResponseStatusCode", out var sc) && sc.ValueKind != JsonValueKind.Null ? sc.GetInt32() : (int?)null;
                var durationMs = e.TryGetProperty("DurationMs", out var dm) ? dm.GetDouble() : 0;
                var error = e.TryGetProperty("ErrorMessage", out var em) && em.ValueKind != JsonValueKind.Null ? em.GetString() : null;
                return new WebhookDeliveryRecord(ts, eventType, success, statusCode, TimeSpan.FromMilliseconds(durationMs), error);
            }).ToList();
        }
        catch { return new List<WebhookDeliveryRecord>(); }
    }
}
