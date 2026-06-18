namespace Application.Abstractions.Notifications;

/// <summary>
/// Main service for creating and managing notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a new notification builder.
    /// </summary>
    INotificationBuilder Create(string notificationTypeCode);

    /// <summary>
    /// Gets unread notification count for a user.
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all notifications as read for a user.
    /// </summary>
    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses a notification.
    /// </summary>
    Task DismissAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired notifications.
    /// </summary>
    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends scheduled notifications that are due.
    /// </summary>
    Task<int> SendScheduledNotificationsAsync(CancellationToken cancellationToken = default);
}
