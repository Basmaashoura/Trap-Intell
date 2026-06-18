using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Notifications;

namespace Trap_Intel.Infrastructure.Notifications.Channels;

internal sealed class SmsNotificationChannel : INotificationChannel
{
    private readonly ILogger<SmsNotificationChannel> _logger;

    public SmsNotificationChannel(ILogger<SmsNotificationChannel> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        // Integration point for Twilio or AWS SNS
        _logger.LogInformation("Sending SMS Notification to User {UserId}: [{Title}] {Message}", 
            notification.UserId, notification.Title, notification.Message);

        return Task.CompletedTask;
    }
}
