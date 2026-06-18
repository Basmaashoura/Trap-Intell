using Domain.Notifications;

namespace Application.Abstractions.Notifications;

/// <summary>
/// Context containing all information needed to build a notification.
/// </summary>
public sealed class NotificationContext
{
    /// <summary>
    /// User IDs to send the notification to.
    /// </summary>
    public List<Guid> RecipientUserIds { get; set; } = [];

    /// <summary>
    /// Data for template placeholders.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = [];

    /// <summary>
    /// Override priority (null = use type default).
    /// </summary>
    public NotificationPriority? Priority { get; set; }

    /// <summary>
    /// Override channels (null = use type default).
    /// </summary>
    public NotificationChannel? Channels { get; set; }

    /// <summary>
    /// Related entity type.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Related entity ID.
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Action URL for deep linking.
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Action button text.
    /// </summary>
    public string? ActionText { get; set; }

    /// <summary>
    /// Group key for collapsing similar notifications.
    /// </summary>
    public string? GroupKey { get; set; }

    /// <summary>
    /// When to send (null = immediately).
    /// </summary>
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// When the notification expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// User who triggered the notification.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// Whether to skip sending (e.g., validation failed).
    /// </summary>
    public bool ShouldSend { get; set; } = true;
}
