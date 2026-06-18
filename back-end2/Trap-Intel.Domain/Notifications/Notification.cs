using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Domain.Notifications.Enums;
using Trap_Intel.Domain.Notifications.Events;

namespace Trap_Intel.Domain.Notifications;

/// <summary>
/// Represents a notification sent to a core system user (In-App Inbox).
/// Re-engineered properly for the modern Trap-Intel Domain.
/// </summary>
public sealed class Notification : AggregateRoot<Guid>
{
    private Notification()
    {
        // Required by EF Core
    }

    private Notification(
        Guid id,
        Guid userId,
        string type, // e.g. "Billing.InvoiceDue" or "Alerts.NewAttack"
        NotificationCategory category,
        NotificationPriority priority,
        string title,
        string message,
        string? linkUri = null,
        string? relatedEntityId = null,
        DateTime? expiresAt = null) 
        : base(id)
    {
        UserId = userId;
        Type = type;
        Category = category;
        Priority = priority;
        Title = title;
        Message = message;
        LinkUri = linkUri;
        RelatedEntityId = relatedEntityId;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        IsRead = false;
        IsDismissed = false;
    }

    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public NotificationCategory Category { get; private set; }
    public NotificationPriority Priority { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? LinkUri { get; private set; }
    public string? RelatedEntityId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public bool IsRead { get; private set; }
    public bool IsDismissed { get; private set; }

    /// <summary>
    /// Checks if this notification has expired based on ExpiresAt.
    /// Default notifications without expiration live forever until dismissed/cleared.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

    /// <summary>
    /// Creates a highly cohesive and robust Notification instance via Factory Method.
    /// Triggers the NotificationCreatedDomainEvent for dispatchers to pick it up (e.g. signalR live send).
    /// </summary>
    public static Result<Notification> Create(
        Guid userId,
        string type,
        string title,
        string message,
        NotificationCategory category = NotificationCategory.System,
        NotificationPriority priority = NotificationPriority.Normal,
        string? linkUri = null,
        string? relatedEntityId = null,
        DateTime? expiresAt = null)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure<Notification>(NotificationErrors.TargetUserRequired);
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<Notification>(NotificationErrors.TitleRequired);
        }

        var notification = new Notification(
            Guid.NewGuid(), 
            userId, 
            type, 
            category, 
            priority, 
            title, 
            message, 
            linkUri, 
            relatedEntityId, 
            expiresAt);

        notification.RaiseDomainEvent(new NotificationCreatedDomainEvent(notification.Id, notification.UserId));

        return Result.Success(notification);
    }

    /// <summary>
    /// Mark the notification as read.
    /// Only triggers the domain event if it was not already read.
    /// </summary>
    public Result MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
            RaiseDomainEvent(new NotificationReadDomainEvent(Id, UserId));
        }

        return Result.Success();
    }

    /// <summary>
    /// Mark the notification as unread.
    /// </summary>
    public void MarkAsUnread()
    {
        IsRead = false;
        ReadAt = null;
        // Business rule: Usually no event triggered for rollback unread
    }

    /// <summary>
    /// Dismiss the notification (hides it from standard Inbox views forever).
    /// </summary>
    public Result Dismiss()
    {
        if (!IsDismissed)
        {
            IsDismissed = true;
            // Best practice: if dismissed, mark it as read automatically.
            if (!IsRead)
            {
                IsRead = true;
                ReadAt = DateTime.UtcNow;
            }
            RaiseDomainEvent(new NotificationDismissedDomainEvent(Id, UserId));
        }

        return Result.Success();
    }
}
