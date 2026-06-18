namespace Domain.Notifications;

/// <summary>
/// Category of notification types for grouping in UI.
/// </summary>
public enum NotificationCategory
{
    /// <summary>
    /// System-wide notifications.
    /// </summary>
    System = 1,

    /// <summary>
    /// Stress analysis and detection notifications.
    /// </summary>
    StressAnalysis = 2,

    /// <summary>
    /// Billing and subscription notifications.
    /// </summary>
    Billing = 5,

    /// <summary>
    /// Security related notifications.
    /// </summary>
    Security = 6,

    /// <summary>
    /// General notifications.
    /// </summary>
    General = 7
}
