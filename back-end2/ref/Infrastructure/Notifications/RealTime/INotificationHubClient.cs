using Application.Abstractions.Notifications;

namespace Infrastructure.Notifications.RealTime;

/// <summary>
/// Client interface for SignalR notification hub.
/// Defines methods that can be called on the client.
/// </summary>
public interface INotificationHubClient
{
    /// <summary>
    /// Called when a new notification is received.
    /// </summary>
    Task ReceiveNotification(RealtimeNotificationMessage notification);

    /// <summary>
    /// Called when a notification is marked as read.
    /// </summary>
    Task NotificationRead(Guid notificationId);

    /// <summary>
    /// Called when all notifications are marked as read.
    /// </summary>
    Task AllNotificationsRead();

    /// <summary>
    /// Called when a notification is dismissed.
    /// </summary>
    Task NotificationDismissed(Guid notificationId);

    /// <summary>
    /// Called when a notification is marked as unread.
    /// </summary>
    Task NotificationUnread(Guid notificationId);

    /// <summary>
    /// Called when the unread count is updated.
    /// </summary>
    Task UnreadCountUpdated(int count);

    /// <summary>
    /// Called when connection is established successfully.
    /// </summary>
    Task ConnectionEstablished(string connectionId);

    /// <summary>
    /// Called in response to a Ping.
    /// </summary>
    Task Pong(DateTime serverTime);
}
