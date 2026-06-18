using Application.Abstractions.Notifications;
using Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications.Channels;

/// <summary>
/// Push notification channel (stub for future FCM/APNs implementation).
/// </summary>
internal sealed class PushNotificationChannel : INotificationChannel
{
    private readonly ILogger<PushNotificationChannel> _logger;

    public PushNotificationChannel(ILogger<PushNotificationChannel> logger)
    {
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Push;

    // TODO: Enable when push notification service is implemented
    public bool IsAvailable => false;

    public Task<bool> DeliverAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        // TODO: Implement push notification delivery
        // This will integrate with FCM (Firebase Cloud Messaging) for Android
        // and APNs (Apple Push Notification service) for iOS

        _logger.LogDebug(
            "Push notification channel not implemented. Skipping notification {NotificationId}",
            notification.Id);

        return Task.FromResult(false);
    }
}
