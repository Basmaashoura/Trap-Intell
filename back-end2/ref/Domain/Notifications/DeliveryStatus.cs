namespace Domain.Notifications;

/// <summary>
/// Status of a notification delivery attempt.
/// </summary>
public enum DeliveryStatus
{
    /// <summary>
    /// Delivery is pending.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Notification was sent successfully.
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Notification was confirmed delivered.
    /// </summary>
    Delivered = 3,

    /// <summary>
    /// Delivery failed.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// User clicked/interacted with the notification.
    /// </summary>
    Clicked = 5,

    /// <summary>
    /// Delivery was skipped (user disabled this channel).
    /// </summary>
    Skipped = 6
}
