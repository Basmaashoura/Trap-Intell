using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts.Enums;
using Trap_Intel.Domain.Alerts.Entities;

namespace Trap_Intel.Domain.Alerts.Events;

/// <summary>
/// Events related to Alert child entities.
/// </summary>

#region Comment Events

/// <summary>
/// Comment added to alert (with full details).
/// </summary>
public record AlertCommentCreatedEvent(
    Guid CommentId,
    Guid AlertId,
    Guid OrganizationId,
    Guid AuthorUserId,
    bool IsInternal,
    bool IsReply,
    Guid? ParentCommentId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Comment edited.
/// </summary>
public record AlertCommentEditedEvent(
    Guid CommentId,
    Guid AlertId,
    Guid OrganizationId,
    Guid EditedByUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Comment deleted.
/// </summary>
public record AlertCommentDeletedEvent(
    Guid CommentId,
    Guid AlertId,
    Guid OrganizationId,
    Guid DeletedByUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Comment restored.
/// </summary>
public record AlertCommentRestoredEvent(
    Guid CommentId,
    Guid AlertId,
    Guid OrganizationId,
    Guid RestoredByUserId,
    DateTime OccurredOn) : IDomainEvent;

#endregion

#region Notification Events

/// <summary>
/// Notification queued for sending.
/// </summary>
public record AlertNotificationQueuedEvent(
    Guid NotificationId,
    Guid AlertId,
    Guid OrganizationId,
    NotificationChannel Channel,
    NotificationTrigger Trigger,
    int RecipientCount,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Notification delivery confirmed.
/// </summary>
public record AlertNotificationDeliveredEvent(
    Guid NotificationId,
    Guid AlertId,
    Guid OrganizationId,
    NotificationChannel Channel,
    string? ExternalMessageId,
    TimeSpan TimeToDelivery,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Notification delivery failed.
/// </summary>
public record AlertNotificationFailedEvent(
    Guid NotificationId,
    Guid AlertId,
    Guid OrganizationId,
    NotificationChannel Channel,
    string FailureReason,
    int RetryCount,
    bool WillRetry,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Notification retry attempted.
/// </summary>
public record AlertNotificationRetryEvent(
    Guid NotificationId,
    Guid AlertId,
    Guid OrganizationId,
    NotificationChannel Channel,
    int RetryNumber,
    int MaxRetries,
    DateTime OccurredOn) : IDomainEvent;

#endregion

#region Escalation Events

/// <summary>
/// Escalation recorded (includes full context).
/// </summary>
public record AlertEscalationRecordedEvent(
    Guid EscalationId,
    Guid AlertId,
    Guid OrganizationId,
    EscalationLevel FromLevel,
    EscalationLevel ToLevel,
    string Reason,
    bool IsAutomatic,
    Guid? EscalatedByUserId,
    string? SLABreached,
    TimeSpan? TimeToEscalate,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Users notified about escalation.
/// </summary>
public record EscalationUsersNotifiedEvent(
    Guid EscalationId,
    Guid AlertId,
    Guid OrganizationId,
    EscalationLevel Level,
    List<Guid> NotifiedUserIds,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// SLA breach detected that triggered escalation.
/// </summary>
public record AlertSLABreachDetectedEvent(
    Guid AlertId,
    Guid OrganizationId,
    string SLAName,
    TimeSpan TargetTime,
    TimeSpan ActualTime,
    EscalationLevel TriggeredEscalationLevel,
    DateTime OccurredOn) : IDomainEvent;

#endregion

#region Action Events

/// <summary>
/// Action recorded on alert.
/// </summary>
public record AlertActionRecordedEvent(
    Guid ActionId,
    Guid AlertId,
    Guid OrganizationId,
    AlertActionType ActionType,
    Guid PerformedByUserId,
    string? Description,
    DateTime OccurredOn) : IDomainEvent;

#endregion

#region Aggregate Events

/// <summary>
/// Alert reopened after being resolved.
/// </summary>
public record AlertReopenedEvent(
    Guid AlertId,
    Guid OrganizationId,
    Guid ReopenedByUserId,
    string Reason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Alert linked to another alert or incident.
/// </summary>
public record AlertLinkedEvent(
    Guid AlertId,
    Guid OrganizationId,
    Guid LinkedToId,
    string LinkedType, // "Alert" or "Incident"
    string Relationship, // "Related", "Duplicate", "Parent", "Child"
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Alert merged with another alert.
/// </summary>
public record AlertMergedEvent(
    Guid PrimaryAlertId,
    Guid MergedAlertId,
    Guid OrganizationId,
    Guid MergedByUserId,
    DateTime OccurredOn) : IDomainEvent;

#endregion
