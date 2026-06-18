namespace Trap_Intel.Domain.Identity.Notifications;

/// <summary>
/// Notification item for in-app display.
/// </summary>
public record InAppNotification
{
    /// <summary>
    /// Unique notification ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// User who receives this notification.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Organization context.
    /// </summary>
    public Guid OrganizationId { get; init; }

    /// <summary>
    /// Notification type.
    /// </summary>
    public NotificationType Type { get; init; }

    /// <summary>
    /// Notification title.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Notification message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// URL to navigate to when clicked.
    /// </summary>
    public string? ActionUrl { get; init; }

    /// <summary>
    /// Whether notification has been read.
    /// </summary>
    public bool IsRead { get; init; }

    /// <summary>
    /// When notification was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When notification was read.
    /// </summary>
    public DateTime? ReadAt { get; init; }

    /// <summary>
    /// Related entity ID (e.g., AlertId, AttackEventId).
    /// </summary>
    public Guid? RelatedEntityId { get; init; }

    /// <summary>
    /// Related entity type.
    /// </summary>
    public string? RelatedEntityType { get; init; }

    /// <summary>
    /// Priority level.
    /// </summary>
    public NotificationPriority Priority { get; init; }

    public InAppNotification(
        Guid id,
        Guid userId,
        Guid organizationId,
        NotificationType type,
        string title,
        string message,
        NotificationPriority priority = NotificationPriority.Normal,
        string? actionUrl = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        Id = id;
        UserId = userId;
        OrganizationId = organizationId;
        Type = type;
        Title = title;
        Message = message;
        Priority = priority;
        ActionUrl = actionUrl;
        RelatedEntityId = relatedEntityId;
        RelatedEntityType = relatedEntityType;
        IsRead = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark as read.
    /// </summary>
    public InAppNotification MarkAsRead() => this with
    {
        IsRead = true,
        ReadAt = DateTime.UtcNow
    };

    /// <summary>
    /// Create alert notification.
    /// </summary>
    public static InAppNotification ForAlert(
        Guid userId,
        Guid organizationId,
        Guid alertId,
        string title,
        string message,
        NotificationPriority priority = NotificationPriority.Normal)
    {
        return new InAppNotification(
            Guid.NewGuid(),
            userId,
            organizationId,
            NotificationType.AlertCreated,
            title,
            message,
            priority,
            $"/alerts/{alertId}",
            alertId,
            "Alert");
    }

    /// <summary>
    /// Create attack notification.
    /// </summary>
    public static InAppNotification ForAttack(
        Guid userId,
        Guid organizationId,
        Guid attackId,
        string title,
        string message,
        NotificationPriority priority = NotificationPriority.High)
    {
        return new InAppNotification(
            Guid.NewGuid(),
            userId,
            organizationId,
            NotificationType.HighSeverityAttack,
            title,
            message,
            priority,
            $"/attacks/{attackId}",
            attackId,
            "AttackEvent");
    }

    /// <summary>
    /// Create threat actor notification.
    /// </summary>
    public static InAppNotification ForThreatActor(
        Guid userId,
        Guid organizationId,
        Guid threatActorId,
        string title,
        string message)
    {
        return new InAppNotification(
            Guid.NewGuid(),
            userId,
            organizationId,
            NotificationType.NewThreatActor,
            title,
            message,
            NotificationPriority.Normal,
            $"/threat-actors/{threatActorId}",
            threatActorId,
            "ThreatActor");
    }

    /// <summary>
    /// Create honeypot notification.
    /// </summary>
    public static InAppNotification ForHoneypot(
        Guid userId,
        Guid organizationId,
        Guid honeypotId,
        NotificationType type,
        string title,
        string message,
        NotificationPriority priority = NotificationPriority.Normal)
    {
        return new InAppNotification(
            Guid.NewGuid(),
            userId,
            organizationId,
            type,
            title,
            message,
            priority,
            $"/honeypots/{honeypotId}",
            honeypotId,
            "Honeypot");
    }

    /// <summary>
    /// Create system notification.
    /// </summary>
    public static InAppNotification ForSystem(
        Guid userId,
        Guid organizationId,
        NotificationType type,
        string title,
        string message,
        string? actionUrl = null)
    {
        return new InAppNotification(
            Guid.NewGuid(),
            userId,
            organizationId,
            type,
            title,
            message,
            NotificationPriority.Normal,
            actionUrl);
    }
}

/// <summary>
/// Priority level for notifications.
/// </summary>
public enum NotificationPriority
{
    /// <summary>Low priority (informational).</summary>
    Low = 0,
    
    /// <summary>Normal priority.</summary>
    Normal = 1,
    
    /// <summary>High priority (important).</summary>
    High = 2,
    
    /// <summary>Urgent priority (requires immediate attention).</summary>
    Urgent = 3,
    
    /// <summary>Critical priority (security incident).</summary>
    Critical = 4
}

/// <summary>
/// Summary of unread notifications for a user.
/// </summary>
public record NotificationSummary
{
    /// <summary>
    /// Total unread notifications.
    /// </summary>
    public int TotalUnread { get; init; }

    /// <summary>
    /// Unread alert notifications.
    /// </summary>
    public int UnreadAlerts { get; init; }

    /// <summary>
    /// Unread attack notifications.
    /// </summary>
    public int UnreadAttacks { get; init; }

    /// <summary>
    /// Unread threat actor notifications.
    /// </summary>
    public int UnreadThreatActors { get; init; }

    /// <summary>
    /// Unread honeypot notifications.
    /// </summary>
    public int UnreadHoneypots { get; init; }

    /// <summary>
    /// Unread system notifications.
    /// </summary>
    public int UnreadSystem { get; init; }

    /// <summary>
    /// Highest priority among unread.
    /// </summary>
    public NotificationPriority HighestPriority { get; init; }

    /// <summary>
    /// Most recent notification timestamp.
    /// </summary>
    public DateTime? MostRecentAt { get; init; }

    public NotificationSummary(
        int totalUnread,
        int unreadAlerts,
        int unreadAttacks,
        int unreadThreatActors,
        int unreadHoneypots,
        int unreadSystem,
        NotificationPriority highestPriority,
        DateTime? mostRecentAt)
    {
        TotalUnread = totalUnread;
        UnreadAlerts = unreadAlerts;
        UnreadAttacks = unreadAttacks;
        UnreadThreatActors = unreadThreatActors;
        UnreadHoneypots = unreadHoneypots;
        UnreadSystem = unreadSystem;
        HighestPriority = highestPriority;
        MostRecentAt = mostRecentAt;
    }

    public static NotificationSummary Empty => new(0, 0, 0, 0, 0, 0, NotificationPriority.Low, null);

    /// <summary>
    /// Whether there are any critical/urgent notifications.
    /// </summary>
    public bool HasUrgent => HighestPriority >= NotificationPriority.Urgent;
}

/// <summary>
/// Email notification data for sending.
/// </summary>
public record EmailNotificationData
{
    /// <summary>
    /// Recipient email.
    /// </summary>
    public string ToEmail { get; init; }

    /// <summary>
    /// Recipient name.
    /// </summary>
    public string ToName { get; init; }

    /// <summary>
    /// Email subject.
    /// </summary>
    public string Subject { get; init; }

    /// <summary>
    /// Template name to use.
    /// </summary>
    public string TemplateName { get; init; }

    /// <summary>
    /// Template data/variables.
    /// </summary>
    public Dictionary<string, object> TemplateData { get; init; }

    /// <summary>
    /// Notification type for tracking.
    /// </summary>
    public NotificationType Type { get; init; }

    /// <summary>
    /// Priority for send order.
    /// </summary>
    public NotificationPriority Priority { get; init; }

    /// <summary>
    /// Whether this is a transactional email (vs marketing).
    /// </summary>
    public bool IsTransactional { get; init; } = true;

    public EmailNotificationData(
        string toEmail,
        string toName,
        string subject,
        string templateName,
        Dictionary<string, object> templateData,
        NotificationType type,
        NotificationPriority priority = NotificationPriority.Normal)
    {
        ToEmail = toEmail;
        ToName = toName;
        Subject = subject;
        TemplateName = templateName;
        TemplateData = templateData;
        Type = type;
        Priority = priority;
    }
}

/// <summary>
/// SMS notification data for sending.
/// </summary>
public record SmsNotificationData
{
    /// <summary>
    /// Recipient phone number.
    /// </summary>
    public string ToPhoneNumber { get; init; }

    /// <summary>
    /// SMS message (max 160 chars recommended).
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Notification type for tracking.
    /// </summary>
    public NotificationType Type { get; init; }

    /// <summary>
    /// Whether this is a critical alert.
    /// </summary>
    public bool IsCritical { get; init; }

    public SmsNotificationData(
        string toPhoneNumber,
        string message,
        NotificationType type,
        bool isCritical = false)
    {
        ToPhoneNumber = toPhoneNumber;
        Message = message.Length > 160 ? message[..157] + "..." : message;
        Type = type;
        IsCritical = isCritical;
    }
}
