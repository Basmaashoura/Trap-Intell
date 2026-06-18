using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Application.Subscriptions.Queries.GetSubscriptionById;

public sealed record SubscriptionQuotaUsageDto(
    int CurrentHoneypots,
    int MaxHoneypots,
    decimal HoneypotUsagePercent,
    decimal CurrentStorageGb,
    decimal MaxStorageGb,
    decimal StorageUsagePercent,
    int CurrentApiCalls,
    int MaxApiCalls,
    decimal ApiCallsUsagePercent,
    bool IsApproachingLimit,
    bool IsOverLimit);

public sealed record SubscriptionDetailDto(
    Guid Id,
    Guid OrganizationId,
    Guid PlanId,
    SubscriptionStatus Status,
    BillingCycle BillingCycle,
    decimal TotalBilled,
    decimal? DiscountApplied,
    DateTime PeriodStartDate,
    DateTime? PeriodEndDate,
    DateTime? RenewalDate,
    bool IsAutoRenew,
    Guid? PaymentMethodId,
    DateTime? CancelledAt,
    string? CancellationReason,
    int HoneypotsUsed,
    decimal StorageUsedGb,
    decimal OverageCharges,
    decimal CalculatedOverageCharges,
    bool HasOverages,
    SubscriptionQuotaUsageDto QuotaUsage,
    DateTime CreatedAt,
    DateTime UpdatedAt);
