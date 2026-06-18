using Trap_Intel.Application.Subscriptions.Queries.GetSubscriptionById;
using Trap_Intel.Domain.Subscriptions.Entities;

namespace Trap_Intel.Application.Subscriptions.Queries.GetSubscriptionUsageInsights;

public sealed record SubscriptionUsageSnapshotDto(
    Guid Id,
    DateTime RecordedAt,
    UsagePeriodType PeriodType,
    int HoneypotsActive,
    decimal StorageUsedGb,
    int ApiCallsCount,
    int ActiveUsers,
    int EventsCaptured,
    decimal? StorageDeltaGb,
    int? HoneypotsDelta);

public sealed record SubscriptionMonthlyUsageDto(
    Guid Id,
    int Year,
    int Month,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int PeakHoneypots,
    decimal PeakStorageGb,
    int TotalApiCalls,
    decimal AverageHoneypots,
    decimal AverageStorageGb,
    int TotalEventsCaptured,
    decimal OverageCharges,
    bool IsBilled,
    Guid? InvoiceId,
    DateTime? FinalizedAt,
    bool IsFinalized);

public sealed record SubscriptionUsageInsightsDto(
    Guid SubscriptionId,
    Guid OrganizationId,
    SubscriptionQuotaUsageDto CurrentQuotaUsage,
    decimal CalculatedOverageCharges,
    bool HasOverages,
    bool CanAddHoneypot,
    bool IsCancellationScheduled,
    IReadOnlyList<SubscriptionUsageSnapshotDto> RecentSnapshots,
    IReadOnlyList<SubscriptionMonthlyUsageDto> MonthlySummaries,
    DateTime UpdatedAt);
