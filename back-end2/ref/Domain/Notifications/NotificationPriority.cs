namespace Domain.Notifications;

/// <summary>
/// Priority level for notifications.
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Low priority - can be batched or delayed.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Normal priority - standard delivery.
    /// </summary>
    Normal = 2,

    /// <summary>
    /// High priority - should be delivered promptly.
    /// </summary>
    High = 3,

    /// <summary>
    /// Urgent priority - immediate delivery, bypasses quiet hours.
    /// </summary>
    Urgent = 4
}
