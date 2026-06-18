using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Domain.Subscriptions.Entities;

namespace Trap_Intel.Infrastructure.Configuration.EntityConfigurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        // Primary Key
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Foreign Keys
        builder.Property(s => s.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(s => s.PlanId)
            .HasColumnName("plan_id")
            .IsRequired();

        builder.Property(s => s.PaymentMethodId)
            .HasColumnName("payment_method_id");

        // Enum: Status
        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Enum: BillingCycle
        builder.Property(s => s.BillingCycle)
            .HasColumnName("billing_cycle")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Boolean Properties
        builder.Property(s => s.IsAutoRenew)
            .HasColumnName("is_auto_renew")
            .HasDefaultValue(true);

        // Timestamps
        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .IsConcurrencyToken();

        // Value Object: SubscriptionPeriod (owned)
        builder.OwnsOne(s => s.Period, period =>
        {
            period.Property(p => p.StartDate)
                .HasColumnName("period_start_date")
                .IsRequired();

            period.Property(p => p.EndDate)
                .HasColumnName("period_end_date");

            period.Property(p => p.RenewalDate)
                .HasColumnName("period_renewal_date");
        });

        // Value Object: BillingInfo (owned)
        builder.OwnsOne(s => s.BillingInfo, billing =>
        {
            billing.Property(b => b.Cycle)
                .HasColumnName("billing_info_cycle")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            billing.Property(b => b.TotalBilled)
                .HasColumnName("billing_info_total_billed")
                .HasPrecision(18, 2)
                .IsRequired();

            billing.Property(b => b.DiscountApplied)
                .HasColumnName("billing_info_discount_applied")
                .HasPrecision(18, 2);
        });

        // Value Object: CancellationInfo (owned, nullable)
        builder.OwnsOne(s => s.CancellationInfo, cancel =>
        {
            cancel.Property(c => c.CancelledAt)
                .HasColumnName("cancellation_at");

            cancel.Property(c => c.Reason)
                .HasColumnName("cancellation_reason")
                .HasMaxLength(1000);
        });

        // Value Object: UsageStatistics (owned)
        builder.OwnsOne(s => s.CurrentUsage, usage =>
        {
            usage.Property(u => u.HoneypotsUsed)
                .HasColumnName("usage_honeypots_used")
                .HasDefaultValue(0);

            usage.Property(u => u.StorageUsedGb)
                .HasColumnName("usage_storage_used_gb")
                .HasPrecision(18, 4)
                .HasDefaultValue(0m);

            usage.Property(u => u.OverageCharges)
                .HasColumnName("usage_overage_charges")
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);
        });

        // Relationship: Quota (one-to-one, owned)
        builder.HasOne(s => s.Quota)
            .WithOne()
            .HasForeignKey<SubscriptionQuotaEntity>(q => q.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship: QuotaHistory (one-to-many)
        builder.HasMany(s => s.QuotaHistory)
            .WithOne()
            .HasForeignKey(q => q.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship: UsageSnapshots (one-to-many)
        builder.HasMany(s => s.UsageSnapshots)
            .WithOne()
            .HasForeignKey(u => u.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship: MonthlySummaries (one-to-many)
        builder.HasMany(s => s.MonthlySummaries)
            .WithOne()
            .HasForeignKey(m => m.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.OrganizationId)
            .HasDatabaseName("ix_subscriptions_organization_id");

        builder.HasIndex(s => s.PlanId)
            .HasDatabaseName("ix_subscriptions_plan_id");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("ix_subscriptions_status");

        builder.HasIndex(s => new { s.OrganizationId, s.Status })
            .HasDatabaseName("ix_subscriptions_org_status");
    }
}

public class SubscriptionQuotaConfiguration : IEntityTypeConfiguration<SubscriptionQuotaEntity>
{
    public void Configure(EntityTypeBuilder<SubscriptionQuotaEntity> builder)
    {
        builder.ToTable("subscription_quotas");

        // Primary Key
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Foreign Keys
        builder.Property(q => q.SubscriptionId)
            .HasColumnName("subscription_id")
            .IsRequired();

        builder.Property(q => q.SourcePlanId)
            .HasColumnName("source_plan_id");

        // Limits
        builder.Property(q => q.MaxHoneypots)
            .HasColumnName("max_honeypots")
            .IsRequired();

        builder.Property(q => q.MaxStorageGb)
            .HasColumnName("max_storage_gb")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(q => q.MaxMonthlyApiCalls)
            .HasColumnName("max_monthly_api_calls")
            .IsRequired();

        builder.Property(q => q.MaxUsers)
            .HasColumnName("max_users")
            .IsRequired();

        // Enforcement & Rates
        builder.Property(q => q.HardLimitEnforced)
            .HasColumnName("hard_limit_enforced")
            .HasDefaultValue(false);

        builder.Property(q => q.OverageHoneypotRate)
            .HasColumnName("overage_honeypot_rate")
            .HasPrecision(18, 2)
            .HasDefaultValue(10m);

        builder.Property(q => q.OverageStorageRatePerGb)
            .HasColumnName("overage_storage_rate_per_gb")
            .HasPrecision(18, 4)
            .HasDefaultValue(0.50m);

        // Effective Period
        builder.Property(q => q.EffectiveFrom)
            .HasColumnName("effective_from")
            .IsRequired();

        builder.Property(q => q.EffectiveTo)
            .HasColumnName("effective_to");

        builder.Property(q => q.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(q => q.SubscriptionId)
            .HasDatabaseName("ix_subscription_quotas_subscription_id");

        builder.HasIndex(q => new { q.SubscriptionId, q.IsActive })
            .HasDatabaseName("ix_subscription_quotas_active");
    }
}

public class UsageSnapshotConfiguration : IEntityTypeConfiguration<UsageSnapshot>
{
    public void Configure(EntityTypeBuilder<UsageSnapshot> builder)
    {
        builder.ToTable("usage_snapshots");

        // Primary Key
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Foreign Key
        builder.Property(u => u.SubscriptionId)
            .HasColumnName("subscription_id")
            .IsRequired();

        // Enum
        builder.Property(u => u.PeriodType)
            .HasColumnName("period_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Usage Metrics
        builder.Property(u => u.HoneypotsActive)
            .HasColumnName("honeypots_active")
            .IsRequired();

        builder.Property(u => u.StorageUsedGb)
            .HasColumnName("storage_used_gb")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(u => u.ApiCallsCount)
            .HasColumnName("api_calls_count")
            .HasDefaultValue(0);

        builder.Property(u => u.ActiveUsers)
            .HasColumnName("active_users")
            .HasDefaultValue(0);

        builder.Property(u => u.EventsCaptured)
            .HasColumnName("events_captured")
            .HasDefaultValue(0);

        // Optional Delta Values
        builder.Property(u => u.HoneypotsDelta)
            .HasColumnName("honeypots_delta");

        builder.Property(u => u.StorageDeltaGb)
            .HasColumnName("storage_delta_gb")
            .HasPrecision(18, 4);

        // Timestamps
        builder.Property(u => u.RecordedAt)
            .HasColumnName("recorded_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(u => u.SubscriptionId)
            .HasDatabaseName("ix_usage_snapshots_subscription_id");

        builder.HasIndex(u => u.RecordedAt)
            .HasDatabaseName("ix_usage_snapshots_recorded_at");

        builder.HasIndex(u => new { u.SubscriptionId, u.PeriodType, u.RecordedAt })
            .HasDatabaseName("ix_usage_snapshots_sub_type_time");
    }
}

public class MonthlyUsageSummaryConfiguration : IEntityTypeConfiguration<MonthlyUsageSummary>
{
    public void Configure(EntityTypeBuilder<MonthlyUsageSummary> builder)
    {
        builder.ToTable("monthly_usage_summaries");

        // Primary Key
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Foreign Key
        builder.Property(m => m.SubscriptionId)
            .HasColumnName("subscription_id")
            .IsRequired();

        // Period
        builder.Property(m => m.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(m => m.Month)
            .HasColumnName("month")
            .IsRequired();

        // Peak Usage
        builder.Property(m => m.PeakHoneypots)
            .HasColumnName("peak_honeypots")
            .HasDefaultValue(0);

        builder.Property(m => m.PeakStorageGb)
            .HasColumnName("peak_storage_gb")
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(m => m.TotalApiCalls)
            .HasColumnName("total_api_calls")
            .HasDefaultValue(0);

        builder.Property(m => m.TotalEventsCaptured)
            .HasColumnName("total_events_captured")
            .HasDefaultValue(0);

        // Averages
        builder.Property(m => m.AverageHoneypots)
            .HasColumnName("avg_honeypots")
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(m => m.AverageStorageGb)
            .HasColumnName("avg_storage_gb")
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        // Overage
        builder.Property(m => m.OverageCharges)
            .HasColumnName("overage_charges")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        // Status
        builder.Property(m => m.IsFinalized)
            .HasColumnName("is_finalized")
            .HasDefaultValue(false);

        builder.Property(m => m.IsBilled)
            .HasColumnName("is_billed")
            .HasDefaultValue(false);

        builder.Property(m => m.InvoiceId)
            .HasColumnName("invoice_id");

        // Timestamps
        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(m => m.FinalizedAt)
            .HasColumnName("finalized_at");

        // Indexes
        builder.HasIndex(m => m.SubscriptionId)
            .HasDatabaseName("ix_monthly_summaries_subscription_id");

        builder.HasIndex(m => new { m.SubscriptionId, m.Year, m.Month })
            .IsUnique()
            .HasDatabaseName("ix_monthly_summaries_sub_year_month");

        builder.HasIndex(m => m.IsFinalized)
            .HasDatabaseName("ix_monthly_summaries_finalized");
    }
}
