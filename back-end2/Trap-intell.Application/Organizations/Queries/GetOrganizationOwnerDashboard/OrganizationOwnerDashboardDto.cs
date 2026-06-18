namespace Trap_Intel.Application.Organizations.Queries.GetOrganizationOwnerDashboard;

public sealed record OrganizationOwnerDashboardDto(
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationStatus,
    bool HasSubscription,
    OrganizationOwnerSubscriptionSummaryDto? Subscription,
    OrganizationOwnerQuotaSummaryDto? Quota,
    OrganizationOwnerAlertSummaryDto Alerts,
    OrganizationOwnerAuditSummaryDto Auditing,
    DateTime GeneratedAtUtc);

public sealed record OrganizationOwnerSubscriptionSummaryDto(
    Guid SubscriptionId,
    Guid PlanId,
    string? PlanName,
    string? PlanType,
    string Status,
    string BillingCycle,
    decimal TotalBilled,
    decimal? DiscountApplied,
    DateTime PeriodStartDate,
    DateTime? PeriodEndDate,
    DateTime? RenewalDate,
    bool IsAutoRenew,
    DateTime UpdatedAt);

public sealed record OrganizationOwnerQuotaSummaryDto(
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
    bool IsOverLimit,
    decimal CalculatedOverageCharges,
    bool HasOverages,
    bool CanAddHoneypot,
    bool IsCancellationScheduled);

public sealed record OrganizationOwnerAlertSummaryDto(
    int TotalActiveAlerts,
    int UnacknowledgedAlerts,
    int CriticalUnresolvedAlerts,
    int EscalatedAlerts,
    int FalsePositivesLastNDays,
    IReadOnlyList<OrganizationOwnerTrendItemDto> AlertsByType,
    IReadOnlyList<OrganizationOwnerTrendItemDto> AlertsBySeverity);

public sealed record OrganizationOwnerAuditSummaryDto(
    int TotalEvents,
    int UnacknowledgedCriticalEvents,
    int HighSeverityEvents,
    IReadOnlyList<OrganizationOwnerAuditResourceItemDto> TopResourceTypes,
    IReadOnlyList<OrganizationOwnerRecentAuditEventDto> RecentCriticalEvents);

public sealed record OrganizationOwnerTrendItemDto(string Category, int Count);

public sealed record OrganizationOwnerAuditResourceItemDto(string ResourceType, int Count);

public sealed record OrganizationOwnerRecentAuditEventDto(
    Guid Id,
    string Action,
    string ResourceType,
    DateTime Timestamp,
    string? Reason);
