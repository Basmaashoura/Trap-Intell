using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Application.Subscriptions.Queries.GetCurrentOrganizationSubscription;

public sealed record SubscriptionSummaryDto(
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
    int HoneypotsUsed,
    decimal StorageUsedGb,
    decimal OverageCharges,
    DateTime UpdatedAt);
