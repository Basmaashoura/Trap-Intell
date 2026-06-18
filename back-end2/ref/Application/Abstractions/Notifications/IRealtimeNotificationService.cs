namespace Application.Abstractions.Notifications;

/// <summary>
/// Service for sending real-time notifications via SignalR.
/// </summary>
public interface IRealtimeNotificationService
{
    /// <summary>
    /// Sends a notification to a specific user.
    /// </summary>
    Task SendToUserAsync(
        Guid userId,
        RealtimeNotificationMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to multiple users.
    /// </summary>
    Task SendToUsersAsync(
        IEnumerable<Guid> userIds,
        RealtimeNotificationMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to all users with a specific role.
    /// </summary>
    Task SendToRoleAsync(
        string roleName,
        RealtimeNotificationMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to all connected users.
    /// </summary>
    Task SendToAllAsync(
        RealtimeNotificationMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies a user that a notification was marked as read.
    /// </summary>
    Task NotifyReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies a user that all notifications were marked as read.
    /// </summary>
    Task NotifyAllReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies a user of their updated unread count.
    /// </summary>
    Task NotifyUnreadCountAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken = default);
}
