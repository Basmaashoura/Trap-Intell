using Trap_Intel.Domain.Alerts.Enums;

namespace Trap_Intel.Domain.Alerts.ValueObjects;

/// <summary>
/// Alert source reference.
/// </summary>
public record AlertSource
{
    public string SourceType { get; init; } = string.Empty;     // "AttackEvent", "Honeypot", "ThreatActor", "System"
    public Guid? SourceId { get; init; }
    public string? SourceName { get; init; }
    public string? IPAddress { get; init; }

    public static AlertSource FromAttackEvent(Guid attackEventId, string? ipAddress = null)
    {
        return new AlertSource
        {
            SourceType = "AttackEvent",
            SourceId = attackEventId,
            IPAddress = ipAddress
        };
    }

    public static AlertSource FromHoneypot(Guid honeypotId, string? honeypotName = null)
    {
        return new AlertSource
        {
            SourceType = "Honeypot",
            SourceId = honeypotId,
            SourceName = honeypotName
        };
    }

    public static AlertSource FromThreatActor(Guid threatActorId, string? alias = null)
    {
        return new AlertSource
        {
            SourceType = "ThreatActor",
            SourceId = threatActorId,
            SourceName = alias
        };
    }

    public static AlertSource FromAuditTrail(Guid auditTrailId, string? component = null)
    {
        return new AlertSource
        {
            SourceType = "AuditTrail",
            SourceId = auditTrailId,
            SourceName = component ?? "Critical Action"
        };
    }

    public static AlertSource FromSystem(string systemComponent)
    {
        return new AlertSource
        {
            SourceType = "System",
            SourceName = systemComponent
        };
    }
}

/// <summary>
/// Notification configuration.
/// </summary>
public record AlertNotificationConfig
{
    public List<NotificationChannel> Channels { get; init; } = new();
    public bool NotifyOnCreate { get; init; } = true;
    public bool NotifyOnEscalation { get; init; } = true;
    public bool NotifyOnResolution { get; init; } = false;
    public List<Guid> NotifyUserIds { get; init; } = new();
    public List<string> NotifyEmails { get; init; } = new();
    public string? WebhookUrl { get; init; }
    public string? SlackChannel { get; init; }

    public static AlertNotificationConfig Default()
    {
        return new AlertNotificationConfig
        {
            Channels = new List<NotificationChannel> { NotificationChannel.Dashboard },
            NotifyOnCreate = true,
            NotifyOnEscalation = true,
            NotifyOnResolution = false
        };
    }

    public static AlertNotificationConfig ForCritical()
    {
        return new AlertNotificationConfig
        {
            Channels = new List<NotificationChannel>
            {
                NotificationChannel.Dashboard,
                NotificationChannel.Email,
                NotificationChannel.SMS
            },
            NotifyOnCreate = true,
            NotifyOnEscalation = true,
            NotifyOnResolution = true
        };
    }
}

/// <summary>
/// Escalation rule.
/// </summary>
public record EscalationRule
{
    public EscalationLevel Level { get; init; }
    public TimeSpan EscalateAfter { get; init; }
    public List<Guid> NotifyUserIds { get; init; } = new();
    public bool SendEmail { get; init; }
    public bool SendSMS { get; init; }

    public static EscalationRule CreateLevel1(TimeSpan after, List<Guid>? userIds = null)
    {
        return new EscalationRule
        {
            Level = EscalationLevel.Level1,
            EscalateAfter = after,
            NotifyUserIds = userIds ?? new(),
            SendEmail = true,
            SendSMS = false
        };
    }

    public static EscalationRule CreateLevel2(TimeSpan after, List<Guid>? userIds = null)
    {
        return new EscalationRule
        {
            Level = EscalationLevel.Level2,
            EscalateAfter = after,
            NotifyUserIds = userIds ?? new(),
            SendEmail = true,
            SendSMS = true
        };
    }
}

/// <summary>
/// Alert action taken.
/// </summary>
public record AlertAction
{
    public string ActionId { get; init; } = string.Empty;
    public string ActionType { get; init; } = string.Empty;     // "Acknowledged", "Commented", "Escalated", "Resolved", etc.
    public string? Description { get; init; }
    public Guid PerformedByUserId { get; init; }
    public string? PerformedByUserName { get; init; }
    public DateTime PerformedAt { get; init; }

    public static AlertAction Create(string actionType, Guid userId, string? description = null)
    {
        return new AlertAction
        {
            ActionId = Guid.NewGuid().ToString("N")[..8],
            ActionType = actionType,
            Description = description,
            PerformedByUserId = userId,
            PerformedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Alert comment.
/// </summary>
public record AlertComment
{
    public string CommentId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public Guid AuthorUserId { get; init; }
    public string? AuthorName { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsInternal { get; init; }       // Internal note vs. public comment

    public static AlertComment Create(string content, Guid userId, bool isInternal = false)
    {
        return new AlertComment
        {
            CommentId = Guid.NewGuid().ToString("N")[..8],
            Content = content,
            AuthorUserId = userId,
            CreatedAt = DateTime.UtcNow,
            IsInternal = isInternal
        };
    }
}

/// <summary>
/// Alert statistics.
/// </summary>
public record AlertStatistics
{
    public int TotalAlerts { get; init; }
    public int NewAlerts { get; init; }
    public int AcknowledgedAlerts { get; init; }
    public int ResolvedAlerts { get; init; }
    public int EscalatedAlerts { get; init; }
    public int CriticalAlerts { get; init; }
    public TimeSpan AverageTimeToAcknowledge { get; init; }
    public TimeSpan AverageTimeToResolve { get; init; }
}

/// <summary>
/// Snooze configuration.
/// </summary>
public record SnoozeConfig
{
    public DateTime SnoozedAt { get; init; }
    public DateTime SnoozeUntil { get; init; }
    public Guid SnoozedByUserId { get; init; }
    public string? Reason { get; init; }

    public bool IsExpired => DateTime.UtcNow >= SnoozeUntil;

    public static SnoozeConfig Create(TimeSpan duration, Guid userId, string? reason = null)
    {
        return new SnoozeConfig
        {
            SnoozedAt = DateTime.UtcNow,
            SnoozeUntil = DateTime.UtcNow.Add(duration),
            SnoozedByUserId = userId,
            Reason = reason
        };
    }
}
