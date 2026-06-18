using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Subscriptions.Entities;

namespace Trap_Intel.Domain.Subscriptions.Events;

/// <summary>
/// Domain events for Quota and Usage tracking.
/// </summary>

/// <summary>
/// Raised when subscription quota is created.
/// </summary>
public record QuotaCreatedEvent(
    Guid QuotaId,
    Guid SubscriptionId,
    int MaxHoneypots,
    decimal MaxStorageGb,
    Guid? SourcePlanId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when subscription quota is changed (plan upgrade/downgrade).
/// </summary>
public record QuotaChangedEvent(
    Guid SubscriptionId,
    Guid OldQuotaId,
    Guid NewQuotaId,
    int OldMaxHoneypots,
    int NewMaxHoneypots,
    decimal OldMaxStorageGb,
    decimal NewMaxStorageGb,
    Guid? NewSourcePlanId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when usage snapshot is recorded.
/// </summary>
public record UsageSnapshotRecordedEvent(
    Guid SnapshotId,
    Guid SubscriptionId,
    UsagePeriodType PeriodType,
    int HoneypotsActive,
    decimal StorageUsedGb,
    int ApiCallsCount,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when usage approaches quota limit (warning threshold).
/// </summary>
public record QuotaWarningEvent(
    Guid SubscriptionId,
    QuotaResourceType ResourceType,
    decimal CurrentUsagePercent,
    decimal WarningThreshold,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when usage exceeds quota limit.
/// </summary>
public record QuotaExceededEvent(
    Guid SubscriptionId,
    QuotaResourceType ResourceType,
    decimal CurrentValue,
    decimal MaxValue,
    bool HardLimitEnforced,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when monthly usage summary is finalized.
/// </summary>
public record MonthlyUsageFinalizedEvent(
    Guid SummaryId,
    Guid SubscriptionId,
    int Year,
    int Month,
    int PeakHoneypots,
    decimal PeakStorageGb,
    int TotalApiCalls,
    decimal OverageCharges,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when monthly usage summary is billed.
/// </summary>
public record MonthlyUsageBilledEvent(
    Guid SummaryId,
    Guid SubscriptionId,
    Guid InvoiceId,
    decimal OverageCharges,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when overage charges are calculated.
/// </summary>
public record OverageChargesCalculatedEvent(
    Guid SubscriptionId,
    int Year,
    int Month,
    decimal HoneypotOverage,
    decimal StorageOverage,
    decimal TotalOverage,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when quota limit enforcement blocks an operation.
/// </summary>
public record QuotaEnforcementBlockedEvent(
    Guid SubscriptionId,
    QuotaResourceType ResourceType,
    string BlockedOperation,
    decimal CurrentValue,
    decimal MaxValue,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Type of quota resource.
/// </summary>
public enum QuotaResourceType
{
    Honeypots = 0,
    Storage = 1,
    ApiCalls = 2,
    Users = 3
}
