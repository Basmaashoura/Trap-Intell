using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Alerts.Entities;

/// <summary>
/// Represents an action taken on an alert.
/// Child entity owned by Alert aggregate.
/// Provides full audit trail of all operations.
/// </summary>
public class AlertActionEntity : Entity<Guid>
{
    // Private constructor for EF
    private AlertActionEntity() { }

    private AlertActionEntity(
        Guid id,
        Guid alertId,
        AlertActionType actionType,
        Guid performedByUserId,
        string? description,
        string? metadata)
        : base(id)
    {
        AlertId = alertId;
        ActionType = actionType;
        PerformedByUserId = performedByUserId;
        Description = description;
        Metadata = metadata;
        PerformedAt = DateTime.UtcNow;
    }

    #region Properties

    /// <summary>
    /// Parent alert ID.
    /// </summary>
    public Guid AlertId { get; private set; }

    /// <summary>
    /// Type of action performed.
    /// </summary>
    public AlertActionType ActionType { get; private set; }

    /// <summary>
    /// User who performed the action.
    /// </summary>
    public Guid PerformedByUserId { get; private set; }

    /// <summary>
    /// Optional description or notes.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// When the action was performed.
    /// </summary>
    public DateTime PerformedAt { get; private set; }

    /// <summary>
    /// Additional metadata as JSON (e.g., old/new values).
    /// </summary>
    public string? Metadata { get; private set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create a new action entity.
    /// </summary>
    public static Result<AlertActionEntity> Create(
        Guid alertId,
        AlertActionType actionType,
        Guid performedByUserId,
        string? description = null,
        string? metadata = null)
    {
        if (alertId == Guid.Empty)
            return Result.Failure<AlertActionEntity>(AlertErrors.InvalidAlertId);

        var action = new AlertActionEntity(
            Guid.NewGuid(),
            alertId,
            actionType,
            performedByUserId,
            description?.Trim(),
            metadata);

        return Result.Success(action);
    }

    /// <summary>
    /// Create action for alert creation.
    /// </summary>
    public static AlertActionEntity ForCreation(Guid alertId, Guid userId)
    {
        return new AlertActionEntity(
            Guid.NewGuid(),
            alertId,
            AlertActionType.Created,
            userId,
            "Alert created",
            null);
    }

    /// <summary>
    /// Create action for acknowledgment.
    /// </summary>
    public static AlertActionEntity ForAcknowledgment(Guid alertId, Guid userId)
    {
        return new AlertActionEntity(
            Guid.NewGuid(),
            alertId,
            AlertActionType.Acknowledged,
            userId,
            "Alert acknowledged",
            null);
    }

    /// <summary>
    /// Create action for investigation start.
    /// </summary>
    public static AlertActionEntity ForInvestigationStarted(Guid alertId, Guid userId)
    {
        return new AlertActionEntity(
            Guid.NewGuid(),
            alertId,
            AlertActionType.InvestigationStarted,
            userId,
            "Investigation started",
            null);
    }

    /// <summary>
    /// Create action for escalation.
    /// </summary>
    public static AlertActionEntity ForEscalation(Guid alertId, Guid userId, string reason)
    {
        return new AlertActionEntity(
            Guid.NewGuid(),
            alertId,
            AlertActionType.Escalated,
            userId,
            reason,
            null);
    }

    /// <summary>
    /// Create action for resolution.
    /// </summary>
    public static AlertActionEntity ForResolution(Guid alertId, Guid userId, string resolution)
    {
        return new AlertActionEntity(
            Guid.NewGuid(),
            alertId,
            AlertActionType.Resolved,
            userId,
            resolution,
            null);
    }

    /// <summary>
    /// Create action for assignment.
    /// </summary>
    public static AlertActionEntity ForAssignment(Guid alertId, Guid assignedByUserId, Guid assignedToUserId)
    {
        return new AlertActionEntity(
            Guid.NewGuid(),
            alertId,
            AlertActionType.Assigned,
            assignedByUserId,
            $"Assigned to user {assignedToUserId}",
            $"{{\"assignedToUserId\":\"{assignedToUserId}\"}}");
    }

    /// <summary>
    /// Create action for snooze.
    /// </summary>
    public static AlertActionEntity ForSnooze(Guid alertId, Guid userId, DateTime snoozeUntil, string? reason)
    {
        return new AlertActionEntity(
            Guid.NewGuid(),
            alertId,
            AlertActionType.Snoozed,
            userId,
            reason ?? $"Snoozed until {snoozeUntil:g}",
            $"{{\"snoozeUntil\":\"{snoozeUntil:O}\"}}");
    }

    /// <summary>
    /// Create action for unsnooze.
    /// </summary>
    public static AlertActionEntity ForUnsnooze(Guid alertId)
    {
        return new AlertActionEntity(
            Guid.NewGuid(),
            alertId,
            AlertActionType.Unsnoozed,
            Guid.Empty,
            "Snooze expired or manually removed",
            null);
    }

    /// <summary>
    /// Create action for false positive.
    /// </summary>
    public static AlertActionEntity ForFalsePositive(Guid alertId, Guid userId, string reason)
    {
        return new AlertActionEntity(
            Guid.NewGuid(),
            alertId,
            AlertActionType.MarkedFalsePositive,
            userId,
            reason,
            null);
    }

    /// <summary>
    /// Create action for severity change.
    /// </summary>
    public static AlertActionEntity ForSeverityChange(
        Guid alertId, 
        Guid userId, 
        string oldSeverity, 
        string newSeverity, 
        string reason)
    {
        return new AlertActionEntity(
            Guid.NewGuid(),
            alertId,
            AlertActionType.SeverityChanged,
            userId,
            reason,
            $"{{\"oldSeverity\":\"{oldSeverity}\",\"newSeverity\":\"{newSeverity}\"}}");
    }

    /// <summary>
    /// Create action for expiration.
    /// </summary>
    public static AlertActionEntity ForExpiration(Guid alertId)
    {
        return new AlertActionEntity(
            Guid.NewGuid(),
            alertId,
            AlertActionType.Expired,
            Guid.Empty,
            "Auto-expired due to age",
            null);
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static AlertActionEntity Reconstruct(
        Guid id,
        Guid alertId,
        AlertActionType actionType,
        Guid performedByUserId,
        string? description,
        DateTime performedAt,
        string? metadata)
    {
        return new AlertActionEntity
        {
            Id = id,
            AlertId = alertId,
            ActionType = actionType,
            PerformedByUserId = performedByUserId,
            Description = description,
            PerformedAt = performedAt,
            Metadata = metadata
        };
    }

    #endregion
}

/// <summary>
/// Types of actions that can be performed on an alert.
/// </summary>
public enum AlertActionType
{
    Created = 0,
    Acknowledged = 1,
    InvestigationStarted = 2,
    Escalated = 3,
    Resolved = 4,
    MarkedFalsePositive = 5,
    Assigned = 6,
    Reassigned = 7,
    Commented = 8,
    Snoozed = 9,
    Unsnoozed = 10,
    SeverityChanged = 11,
    PriorityChanged = 12,
    Expired = 13,
    Reopened = 14,
    NotificationSent = 15,
    AttachmentAdded = 16,
    LinkedToIncident = 17,
    Custom = 99
}
