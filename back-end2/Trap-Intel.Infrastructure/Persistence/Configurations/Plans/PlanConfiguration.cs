using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Plans.ValueObjects;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Plans;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();

        builder.Property(p => p.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.CustomizationLevel)
            .HasColumnName("customization_level")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // SupportTier (owned)
        builder.OwnsOne(p => p.SupportTier, support =>
        {
            support.Property(s => s.Level)
                .HasColumnName("support_level")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            support.Property(s => s.ResponseTimeMinutes).HasColumnName("support_response_time_minutes").IsRequired();
            support.Property(s => s.IncludesDedicatedManager).HasColumnName("support_includes_dedicated_manager").HasDefaultValue(false);
        });

        // ComplianceConfig (owned)
        builder.OwnsOne(p => p.ComplianceConfig, compliance =>
        {
            compliance.Property(c => c.Level)
                .HasColumnName("compliance_level")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            compliance.Property(c => c.RequiredCertifications)
                .HasColumnName("compliance_certifications")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>());
            compliance.Property(c => c.AuditingIncluded).HasColumnName("compliance_auditing_included").HasDefaultValue(false);
        });

        // AIFeatures (owned, nullable)
        builder.OwnsOne(p => p.AIFeatures, ai =>
        {
            ai.Property(a => a.ThreatAnalysis).HasColumnName("ai_threat_analysis").HasDefaultValue(false);
            ai.Property(a => a.AutomatedDetection).HasColumnName("ai_automated_detection").HasDefaultValue(false);
            ai.Property(a => a.PredictiveAnalytics).HasColumnName("ai_predictive_analytics").HasDefaultValue(false);
            ai.Property(a => a.CustomModels).HasColumnName("ai_custom_models").HasDefaultValue(false);
        });

        // ThreatIntelligence (owned, nullable)
        builder.OwnsOne(p => p.ThreatIntelligence, ti =>
        {
            ti.Property(t => t.IsIncluded).HasColumnName("threat_intel_included").HasDefaultValue(false);
            ti.Property(t => t.DataSources)
                .HasColumnName("threat_intel_data_sources")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>());
            ti.Property(t => t.UpdateFrequencyHours).HasColumnName("threat_intel_update_hours").HasDefaultValue(24);
        });

        // QuotaDefinition (owned, nullable)
        builder.OwnsOne(p => p.QuotaDefinition, quota =>
        {
            quota.Property(q => q.MaxHoneypots).HasColumnName("quota_max_honeypots");
            quota.Property(q => q.MaxStorageGb).HasColumnName("quota_max_storage_gb").HasPrecision(18, 4);
            quota.Property(q => q.MaxMonthlyApiCalls).HasColumnName("quota_max_api_calls");
            quota.Property(q => q.MaxUsers).HasColumnName("quota_max_users");
            quota.Property(q => q.MaxAttackEventsRetained).HasColumnName("quota_max_events_retained");
            quota.Property(q => q.DataRetentionDays).HasColumnName("quota_data_retention_days");
            quota.Property(q => q.MaxMonthlyReports).HasColumnName("quota_max_reports");
            quota.Property(q => q.MaxWebhooks).HasColumnName("quota_max_webhooks");
            quota.Property(q => q.MaxApiKeys).HasColumnName("quota_max_api_keys");
            quota.Property(q => q.HardLimitEnforced).HasColumnName("quota_hard_limit_enforced").HasDefaultValue(false);
            quota.Property(q => q.OverageHoneypotRate).HasColumnName("quota_overage_honeypot_rate").HasPrecision(18, 2);
            quota.Property(q => q.OverageStorageRatePerGb).HasColumnName("quota_overage_storage_rate").HasPrecision(18, 4);
            quota.Property(q => q.OverageApiCallRatePer1000).HasColumnName("quota_overage_api_rate").HasPrecision(18, 4);
        });

        // Pricing as JSONB
        builder.Property(p => p.Pricing)
            .HasColumnName("pricing")
            .HasColumnType("jsonb")
            .HasConversion(
                v => SerializePricing(v),
                v => DeserializePricing(v),
                new ValueComparer<IReadOnlyDictionary<BillingCycle, PlanPrice>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)));

        // Features as JSONB
        builder.Property(p => p.Features)
            .HasColumnName("features")
            .HasColumnType("jsonb")
            .HasConversion(
                v => SerializeFeatures(v),
                v => DeserializeFeatures(v),
                new ValueComparer<IReadOnlyList<PlanFeature>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        // Indexes
        builder.HasIndex(p => p.Name).IsUnique().HasDatabaseName("ix_plans_name_unique");
        builder.HasIndex(p => p.Type).HasDatabaseName("ix_plans_type");
        builder.HasIndex(p => p.IsActive).HasDatabaseName("ix_plans_active");
    }

    private static string SerializePricing(IReadOnlyDictionary<BillingCycle, PlanPrice> pricing)
    {
        var dict = pricing.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp => new { kvp.Value.Amount, kvp.Value.Currency, kvp.Value.SetupFee });
        return JsonSerializer.Serialize(dict);
    }

    private static IReadOnlyDictionary<BillingCycle, PlanPrice> DeserializePricing(string json)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (dict == null) return new Dictionary<BillingCycle, PlanPrice>();

            var result = new Dictionary<BillingCycle, PlanPrice>();
            foreach (var kvp in dict)
            {
                if (Enum.TryParse<BillingCycle>(kvp.Key, out var cycle))
                {
                    var amount = kvp.Value.GetProperty("Amount").GetDecimal();
                    var currency = kvp.Value.TryGetProperty("Currency", out var curr) ? curr.GetString() ?? "USD" : "USD";
                    var setupFee = kvp.Value.TryGetProperty("SetupFee", out var fee) ? fee.GetDecimal() : 0m;
                    result[cycle] = new PlanPrice(amount, currency, setupFee);
                }
            }
            return result;
        }
        catch { return new Dictionary<BillingCycle, PlanPrice>(); }
    }

    private static string SerializeFeatures(IReadOnlyList<PlanFeature> features)
    {
        var list = features.Select(f => new
        {
            f.Code, f.Name, f.Description, Category = f.Category.ToString(),
            f.IsEnabled, f.LimitValue, f.LimitUnit, f.IsPremium, f.SortOrder
        });
        return JsonSerializer.Serialize(list);
    }

    private static IReadOnlyList<PlanFeature> DeserializeFeatures(string json)
    {
        try
        {
            var elements = JsonSerializer.Deserialize<List<JsonElement>>(json);
            if (elements == null) return new List<PlanFeature>();

            var result = new List<PlanFeature>();
            foreach (var e in elements)
            {
                var code = e.GetProperty("Code").GetString() ?? "";
                var name = e.GetProperty("Name").GetString() ?? "";
                var description = e.TryGetProperty("Description", out var desc) ? desc.GetString() ?? "" : "";
                var categoryStr = e.GetProperty("Category").GetString() ?? "Honeypots";
                var isEnabled = e.TryGetProperty("IsEnabled", out var en) && en.GetBoolean();
                int? limitValue = e.TryGetProperty("LimitValue", out var lv) && lv.ValueKind != JsonValueKind.Null ? lv.GetInt32() : null;
                var limitUnit = e.TryGetProperty("LimitUnit", out var lu) ? lu.GetString() : null;
                var isPremium = e.TryGetProperty("IsPremium", out var prem) && prem.GetBoolean();
                var sortOrder = e.TryGetProperty("SortOrder", out var so) ? so.GetInt32() : 0;

                if (Enum.TryParse<FeatureCategory>(categoryStr, out var category))
                {
                    result.Add(new PlanFeature(code, name, description, category, isEnabled, limitValue, limitUnit, isPremium, sortOrder));
                }
            }
            return result;
        }
        catch { return new List<PlanFeature>(); }
    }
}
