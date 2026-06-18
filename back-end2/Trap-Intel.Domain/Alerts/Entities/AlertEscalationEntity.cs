using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts.Enums;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Alerts.Entities;

/// <summary>
/// Represents an escalation event for an alert.
/// Child entity owned by Alert aggregate.
/// Provides full escalation audit trail.
/// </summary>
public class AlertEscalationEntity : Entity<Guid>
{
    private List<Guid> _notifiedUserIds = new();

    // Private constructor for EF
    private AlertEscalationEntity() { }

    private AlertEscalationEntity(
        Guid id,
        Guid alertId,
        EscalationLevel fromLevel,
        EscalationLevel toLevel,
        string reason,
        Guid? escalatedByUserId,
        bool isAutomatic)
        : base(id)
    {
        AlertId = alertId;
        FromLevel = fromLevel;
        ToLevel = toLevel;
        Reason = reason;
        EscalatedByUserId = escalatedByUserId;
        IsAutomatic = isAutomatic;
        EscalatedAt = DateTime.UtcNow;
    }

    #region Properties

    /// <summary>
    /// Parent alert ID.
    /// </summary>
    public Guid AlertId { get; private set; }

    /// <summary>
    /// Escalation level before this escalation.
    /// </summary>
    public EscalationLevel FromLevel { get; private set; }

    /// <summary>
    /// Escalation level after this escalation.
    /// </summary>
    public EscalationLevel ToLevel { get; private set; }

    /// <summary>
    /// Reason for escalation.
    /// </summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>
    /// User who triggered the escalation (null if automatic).
    /// </summary>
    public Guid? EscalatedByUserId { get; private set; }

    /// <summary>
    /// Whether this was an automatic escalation (SLA breach, etc.).
    /// </summary>
    public bool IsAutomatic { get; private set; }

    /// <summary>
    /// When the escalation occurred.
    /// </summary>
    public DateTime EscalatedAt { get; private set; }

    /// <summary>
    /// Users who were notified of this escalation.
    /// </summary>
    public IReadOnlyList<Guid> NotifiedUserIds => _notifiedUserIds.AsReadOnly();

    /// <summary>
    /// Time elapsed since alert creation when escalated.
    /// </summary>
    public TimeSpan? TimeToEscalate { get; private set; }

    /// <summary>
    /// SLA target that was breached (if automatic).
    /// </summary>
    public string? SLABreached { get; private set; }

    /// <summary>
    /// Additional context/metadata.
    /// </summary>
    public string? Metadata { get; private set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create a manual escalation.
    /// </summary>
    public static Result<AlertEscalationEntity> CreateManual(
        Guid alertId,
        EscalationLevel fromLevel,
        EscalationLevel toLevel,
        string reason,
        Guid escalatedByUserId,
        DateTime alertCreatedAt)
    {
        if (alertId == Guid.Empty)
            return Result.Failure<AlertEscalationEntity>(AlertErrors.InvalidAlertId);

        if (escalatedByUserId == Guid.Empty)
            return Result.Failure<AlertEscalationEntity>(AlertErrors.InvalidUserId);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure<AlertEscalationEntity>(AlertErrors.InvalidReason);

        if (toLevel <= fromLevel)
            return Result.Failure<AlertEscalationEntity>(AlertErrors.InvalidEscalationLevel);

        var escalation = new AlertEscalationEntity(
            Guid.NewGuid(),
            alertId,
            fromLevel,
            toLevel,
            reason.Trim(),
            escalatedByUserId,
            isAutomatic: false)
        {
            TimeToEscalate = DateTime.UtcNow - alertCreatedAt
        };

        return Result.Success(escalation);
    }

    /// <summary>
    /// Create an automatic escalation (SLA breach).
    /// </summary>
    public static Result<AlertEscalationEntity> CreateAutomatic(
        Guid alertId,
        EscalationLevel fromLevel,
        EscalationLevel toLevel,
        string slaBreached,
        DateTime alertCreatedAt)
    {
        if (alertId == Guid.Empty)
            return Result.Failure<AlertEscalationEntity>(AlertErrors.InvalidAlertId);

        if (string.IsNullOrWhiteSpace(slaBreached))
            return Result.Failure<AlertEscalationEntity>(AlertErrors.InvalidSLAName);

        if (toLevel <= fromLevel)
            return Result.Failure<AlertEscalationEntity>(AlertErrors.InvalidEscalationLevel);

        var escalation = new AlertEscalationEntity(
            Guid.NewGuid(),
            alertId,
            fromLevel,
            toLevel,
            $"Automatic escalation due to SLA breach: {slaBreached}",
            escalatedByUserId: null,
            isAutomatic: true)
        {
            TimeToEscalate = DateTime.UtcNow - alertCreatedAt,
            SLABreached = slaBreached
        };

        return Result.Success(escalation);
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static AlertEscalationEntity Reconstruct(
        Guid id,
        Guid alertId,
        EscalationLevel fromLevel,
        EscalationLevel toLevel,
        string reason,
        Guid? escalatedByUserId,
        bool isAutomatic,
        DateTime escalatedAt,
        List<Guid>? notifiedUserIds,
        TimeSpan? timeToEscalate,
        string? slaBreached,
        string? metadata)
    {
        return new AlertEscalationEntity
        {
            Id = id,
            AlertId = alertId,
            FromLevel = fromLevel,
            ToLevel = toLevel,
            Reason = reason,
            EscalatedByUserId = escalatedByUserId,
            IsAutomatic = isAutomatic,
            EscalatedAt = escalatedAt,
            _notifiedUserIds = notifiedUserIds ?? new(),
            TimeToEscalate = timeToEscalate,
            SLABreached = slaBreached,
            Metadata = metadata
        };
    }

    #endregion

    #region Domain Behaviors

    /// <summary>
    /// Record that a user was notified.
    /// </summary>
    public void AddNotifiedUser(Guid userId)
    {
        if (userId != Guid.Empty && !_notifiedUserIds.Contains(userId))
        {
            _notifiedUserIds.Add(userId);
        }
    }

    /// <summary>
    /// Add multiple notified users.
    /// </summary>
    public void AddNotifiedUsers(IEnumerable<Guid> userIds)
    {
        foreach (var userId in userIds.Where(id => id != Guid.Empty))
        {
            if (!_notifiedUserIds.Contains(userId))
            {
                _notifiedUserIds.Add(userId);
            }
        }
    }

    /// <summary>
    /// Set metadata.
    /// </summary>
    public void SetMetadata(string metadata)
    {
        Metadata = metadata;
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Get the number of levels escalated.
    /// </summary>
    public int GetLevelsEscalated() => (int)ToLevel - (int)FromLevel;

    /// <summary>
    /// Check if this was a significant escalation (2+ levels).
    /// </summary>
    public bool IsSignificantEscalation() => GetLevelsEscalated() >= 2;

    /// <summary>
    /// Check if escalation was to executive level.
    /// </summary>
    public bool IsExecutiveEscalation() => ToLevel >= EscalationLevel.Level4;

    /// <summary>
    /// Check if escalation was to external team.
    /// </summary>
    public bool IsExternalEscalation() => ToLevel == EscalationLevel.External;

    #endregion
}
