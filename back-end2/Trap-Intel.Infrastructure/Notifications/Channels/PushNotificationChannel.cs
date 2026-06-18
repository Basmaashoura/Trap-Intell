using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Notifications;

namespace Trap_Intel.Infrastructure.Notifications.Channels;

internal sealed class PushNotificationChannel : INotificationChannel
{
    private readonly ILogger<PushNotificationChannel> _logger;

    public PushNotificationChannel(ILogger<PushNotificationChannel> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        // Integration point for FCM (Firebase Cloud Messaging) or APNS
        _logger.LogInformation("Sending PUSH Notification to User {UserId}: [{Title}] {Message}", 
            notification.UserId, notification.Title, notification.Message);

        return Task.CompletedTask;
    }
}
