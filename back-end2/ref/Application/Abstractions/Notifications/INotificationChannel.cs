using Domain.Notifications;

namespace Application.Abstractions.Notifications;

/// <summary>
/// Interface for a notification delivery channel.
/// </summary>
public interface INotificationChannel
{
    /// <summary>
    /// The channel this implementation handles.
    /// </summary>
    NotificationChannel Channel { get; }

    /// <summary>
    /// Delivers a notification through this channel.
    /// </summary>
    /// <param name="notification">The notification to deliver.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if delivery was successful.</returns>
    Task<bool> DeliverAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if this channel is available/configured.
    /// </summary>
    bool IsAvailable { get; }
}
