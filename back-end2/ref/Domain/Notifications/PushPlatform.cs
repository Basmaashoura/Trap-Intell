namespace Domain.Notifications;

/// <summary>
/// Platform for push notifications.
/// </summary>
public enum PushPlatform
{
    /// <summary>
    /// Web browser push notifications.
    /// </summary>
    Web = 1,

    /// <summary>
    /// iOS push notifications (APNs).
    /// </summary>
    iOS = 2,

    /// <summary>
    /// Android push notifications (FCM).
    /// </summary>
    Android = 3
}
