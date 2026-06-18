using Domain.Users;
using SharedKernel;

#pragma warning disable CA1054 // URI parameters should not be strings

namespace Domain.Notifications;

/// <summary>
/// Represents a notification sent to a user.
/// </summary>
public sealed class Notification : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The user who receives this notification.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The type of this notification.
    /// </summary>
    public Guid TypeId { get; private set; }

    /// <summary>
    /// The rendered title (after template processing).
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// The rendered message body (after template processing).
    /// </summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Priority of this notification.
    /// </summary>
    public NotificationPriority Priority { get; private set; }

    /// <summary>
    /// The type of entity this notification relates to (e.g., "StressSession", "User").
    /// </summary>
    public string? EntityType { get; private set; }

    /// <summary>
    /// The ID of the related entity.
    /// </summary>
    public Guid? EntityId { get; private set; }

    /// <summary>
    /// URL for the action button (deep link).
    /// </summary>
    public string? ActionUrl { get; private set; }

    /// <summary>
    /// Text for the action button.
    /// </summary>
    public string? ActionText { get; private set; }

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; private set; }

    /// <summary>
    /// Key for grouping similar notifications (e.g., "task-comments-{taskId}").
    /// </summary>
    public string? GroupKey { get; private set; }

    /// <summary>
    /// Whether this notification has been read.
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    /// When this notification was read.
    /// </summary>
    public DateTime? ReadAt { get; private set; }

    /// <summary>
    /// Whether this notification has been dismissed.
    /// </summary>
    public bool IsDismissed { get; private set; }

    /// <summary>
    /// When this notification was dismissed.
    /// </summary>
    public DateTime? DismissedAt { get; private set; }

    /// <summary>
    /// Whether this notification has been archived.
    /// </summary>
    public bool IsArchived { get; private set; }

    /// <summary>
    /// When this notification was archived.
    /// </summary>
    public DateTime? ArchivedAt { get; private set; }

    /// <summary>
    /// When this notification should be sent (for scheduled notifications).
    /// </summary>
    public DateTime? ScheduledFor { get; private set; }

    /// <summary>
    /// When this notification expires and should be auto-dismissed.
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// When this notification was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// The user who triggered this notification (null for system notifications).
    /// </summary>
    public Guid? CreatedByUserId { get; private set; }

    // Navigation properties
    public User? User { get; init; }
    public NotificationType? Type { get; init; }
    public User? CreatedBy { get; init; }
    public List<NotificationDelivery> Deliveries { get; private set; } = [];

    private Notification()
    {
    }

    public static Notification Create(
        Guid userId,
        Guid typeId,
        string title,
        string message,
        NotificationPriority priority,
        string? entityType = null,
        Guid? entityId = null,
        string? actionUrl = null,
        string? actionText = null,
        string? metadata = null,
        string? groupKey = null,
        DateTime? scheduledFor = null,
        DateTime? expiresAt = null,
        Guid? createdByUserId = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TypeId = typeId,
            Title = title,
            Message = message,
            Priority = priority,
            EntityType = entityType,
            EntityId = entityId,
            ActionUrl = actionUrl,
            ActionText = actionText,
            Metadata = metadata,
            GroupKey = groupKey,
            IsRead = false,
            IsDismissed = false,
            IsArchived = false,
            ScheduledFor = scheduledFor,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        notification.Raise(new NotificationCreatedDomainEvent(notification.Id, userId, typeId));

        return notification;
    }

    public void ScheduleFor(DateTime sendAt)
    {
        ScheduledFor = sendAt;
    }

    public void MarkAsRead()
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAt = DateTime.UtcNow;

        Raise(new NotificationReadDomainEvent(Id, UserId));
    }

    public void MarkAsUnread()
    {
        IsRead = false;
        ReadAt = null;
    }

    public void Dismiss()
    {
        if (IsDismissed)
        {
            return;
        }

        IsDismissed = true;
        DismissedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        if (IsArchived)
        {
            return;
        }

        IsArchived = true;
        ArchivedAt = DateTime.UtcNow;
    }

    public void AddDelivery(NotificationDelivery delivery)
    {
        Deliveries.Add(delivery);
    }

    /// <summary>
    /// Checks if this notification is ready to be sent (for scheduled notifications).
    /// </summary>
    public bool IsReadyToSend() =>
        ScheduledFor is null || ScheduledFor <= DateTime.UtcNow;

    /// <summary>
    /// Checks if this notification has expired.
    /// </summary>
    public bool IsExpired() =>
        ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
}
