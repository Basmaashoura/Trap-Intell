using Application.Abstractions.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications.BackgroundServices;

/// <summary>
/// Background service for cleaning up expired notifications.
/// </summary>
internal sealed class NotificationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public NotificationCleanupService(
        IServiceProvider serviceProvider,
        ILogger<NotificationCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredNotificationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during notification cleanup");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Notification cleanup service stopped");
    }

    private async Task CleanupExpiredNotificationsAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        INotificationService notificationService = scope.ServiceProvider
            .GetRequiredService<INotificationService>();

        int cleanedCount = await notificationService.CleanupExpiredAsync(cancellationToken);

        if (cleanedCount > 0)
        {
            _logger.LogInformation(
                "Cleaned up {Count} expired notifications",
                cleanedCount);
        }
    }
}
