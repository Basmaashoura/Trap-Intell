using Application.Abstractions.Notifications;
using Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications.Channels;

/// <summary>
/// In-app notification channel using SignalR.
/// </summary>
internal sealed class InAppNotificationChannel : INotificationChannel
{
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly ILogger<InAppNotificationChannel> _logger;

    public InAppNotificationChannel(
        IRealtimeNotificationService realtimeService,
        ILogger<InAppNotificationChannel> logger)
    {
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.InApp;

    public bool IsAvailable => true;

    public async Task<bool> DeliverAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = RealtimeNotificationMessage.FromNotification(notification, notification.Type);
            await _realtimeService.SendToUserAsync(notification.UserId, message, cancellationToken);

            _logger.LogDebug(
                "Delivered in-app notification {NotificationId} to user {UserId}",
                notification.Id,
                notification.UserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to deliver in-app notification {NotificationId} to user {UserId}",
                notification.Id,
                notification.UserId);

            return false;
        }
    }
}
