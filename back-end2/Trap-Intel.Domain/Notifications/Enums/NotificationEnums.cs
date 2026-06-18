namespace Trap_Intel.Domain.Notifications.Enums;

/// <summary>
/// Status of a notification delivery attempt.
/// </summary>
public enum DeliveryStatus
{
    Pending = 0,
    Delivered = 1,
    Failed = 2
}

/// <summary>
/// Category of the notification.
/// </summary>
public enum NotificationCategory
{
    System = 0,
    Security = 1,
    Billing = 2,
    Team = 3,
    Alert = 4
}

/// <summary>
/// Channels over which a notification can be delivered.
/// </summary>
public enum NotificationChannel
{
    InApp = 0,
    Email = 1,
    Sms = 2,
    Push = 3,
    Slack = 4,
    Teams = 5
}

/// <summary>
/// Priority level of the notification.
/// </summary>
public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Mobile operating system platform for push notifications.
/// </summary>
public enum PushPlatform
{
    Unknown = 0,
    Apple = 1,      // APNS
    Android = 2,    // FCM
    Web = 3         // Web Push
}
