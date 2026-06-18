using Application.Abstractions.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications.BackgroundServices;

/// <summary>
/// Background service for sending scheduled notifications.
/// </summary>
internal sealed class ScheduledNotificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledNotificationService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public ScheduledNotificationService(
        IServiceProvider serviceProvider,
        ILogger<ScheduledNotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled notification service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendScheduledNotificationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending scheduled notifications");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Scheduled notification service stopped");
    }

    private async Task SendScheduledNotificationsAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        INotificationService notificationService = scope.ServiceProvider
            .GetRequiredService<INotificationService>();

        int sentCount = await notificationService.SendScheduledNotificationsAsync(cancellationToken);

        if (sentCount > 0)
        {
            _logger.LogInformation(
                "Sent {Count} scheduled notifications",
                sentCount);
        }
    }
}
