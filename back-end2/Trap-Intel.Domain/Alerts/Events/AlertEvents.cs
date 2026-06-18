using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts.Enums;

namespace Trap_Intel.Domain.Alerts.Events;

/// <summary>
/// Alert created.
/// </summary>
public record AlertCreatedEvent(
    Guid AlertId,
    Guid OrganizationId,
    AlertType AlertType,
    AlertSeverity Severity,
    string Title,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Alert acknowledged by user.
/// </summary>
public record AlertAcknowledgedEvent(
    Guid AlertId,
    Guid OrganizationId,
    Guid AcknowledgedByUserId,
    TimeSpan TimeToAcknowledge,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Alert investigation started.
/// </summary>
public record AlertInvestigationStartedEvent(
    Guid AlertId,
    Guid OrganizationId,
    Guid InvestigatorUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Alert escalated.
/// </summary>
public record AlertEscalatedEvent(
    Guid AlertId,
    Guid OrganizationId,
    EscalationLevel OldLevel,
    EscalationLevel NewLevel,
    string Reason,
    Guid? EscalatedByUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Alert resolved.
/// </summary>
public record AlertResolvedEvent(
    Guid AlertId,
    Guid OrganizationId,
    Guid ResolvedByUserId,
    string Resolution,
    TimeSpan TimeToResolve,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Alert marked as false positive.
/// </summary>
public record AlertMarkedFalsePositiveEvent(
    Guid AlertId,
    Guid OrganizationId,
    Guid MarkedByUserId,
    string Reason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Alert snoozed.
/// </summary>
public record AlertSnoozedEvent(
    Guid AlertId,
    Guid OrganizationId,
    Guid SnoozedByUserId,
    DateTime SnoozeUntil,
    string? Reason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Alert snooze expired - alert reactivated.
/// </summary>
public record AlertSnoozeExpiredEvent(
    Guid AlertId,
    Guid OrganizationId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Alert comment added.
/// </summary>
public record AlertCommentAddedEvent(
    Guid AlertId,
    Guid OrganizationId,
    Guid CommentAuthorUserId,
    bool IsInternal,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Alert assigned to user.
/// </summary>
public record AlertAssignedEvent(
    Guid AlertId,
    Guid OrganizationId,
    Guid? OldAssigneeUserId,
    Guid NewAssigneeUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Alert severity changed.
/// </summary>
public record AlertSeverityChangedEvent(
    Guid AlertId,
    Guid OrganizationId,
    AlertSeverity OldSeverity,
    AlertSeverity NewSeverity,
    string Reason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Alert expired (auto-closed).
/// </summary>
public record AlertExpiredEvent(
    Guid AlertId,
    Guid OrganizationId,
    TimeSpan AgeAtExpiration,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Notification sent for alert.
/// </summary>
public record AlertNotificationSentEvent(
    Guid AlertId,
    Guid OrganizationId,
    NotificationChannel Channel,
    List<string> Recipients,
    DateTime OccurredOn) : IDomainEvent;
