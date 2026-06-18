using Application.Abstractions.Notifications;
using Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications.Channels;

/// <summary>
/// Email notification channel (stub for future implementation).
/// </summary>
internal sealed class EmailNotificationChannel : INotificationChannel
{
    private readonly ILogger<EmailNotificationChannel> _logger;

    public EmailNotificationChannel(ILogger<EmailNotificationChannel> logger)
    {
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Email;

    // TODO: Enable when email notification service is implemented
    public bool IsAvailable => false;

    public Task<bool> DeliverAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        // TODO: Implement email notification delivery
        // This will integrate with IEmailService to send notification emails

        _logger.LogDebug(
            "Email notification channel not implemented. Skipping notification {NotificationId}",
            notification.Id);

        return Task.FromResult(false);
    }
}
