using Domain.Notifications;

namespace Application.Abstractions.Notifications;

/// <summary>
/// Dispatches notifications to multiple channels.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Dispatches a notification to all applicable channels.
    /// </summary>
    /// <param name="notification">The notification to dispatch.</param>
    /// <param name="channels">The channels to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of channel to success status.</returns>
    Task<Dictionary<NotificationChannel, bool>> DispatchAsync(
        Notification notification,
        NotificationChannel channels,
        CancellationToken cancellationToken = default);
}
