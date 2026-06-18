namespace Domain.Notifications;

/// <summary>
/// Channels through which notifications can be delivered.
/// This is a flags enum to allow multiple channels.
/// </summary>
[Flags]
public enum NotificationChannel
{
    /// <summary>
    /// No channel specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// In-app notification via SignalR.
    /// </summary>
    InApp = 1,

    /// <summary>
    /// Email notification.
    /// </summary>
    Email = 2,

    /// <summary>
    /// Push notification (FCM/APNs).
    /// </summary>
    Push = 4,

    /// <summary>
    /// SMS notification.
    /// </summary>
    Sms = 8,

    /// <summary>
    /// All channels.
    /// </summary>
    All = InApp | Email | Push | Sms
}
