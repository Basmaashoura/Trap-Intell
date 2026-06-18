using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Alerts.BackgroundServices;

internal sealed class AlertMaintenanceBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlertMaintenanceBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

    public AlertMaintenanceBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AlertMaintenanceBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlertMaintenanceBackgroundService is starting.");

        // Wait a little before starting to allow app initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during alert maintenance.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task PerformMaintenanceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var alertRepository = scope.ServiceProvider.GetRequiredService<IAlertRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.LogInformation("Starting scheduled alert maintenance (auto-expire & unsnooze).");

        int expiredCount = 0;
        int unsnoozedCount = 0;

        // 1. Process Expired Alerts
        var expiredAlerts = await alertRepository.GetExpiredAlertsAsync(cancellationToken);
        foreach (var alert in expiredAlerts)
        {
            var expireResult = alert.Expire();
            if (expireResult.IsSuccess)
            {
                expiredCount++;
                await alertRepository.UpdateAsync(alert, cancellationToken);
            }
        }

        // 2. Process Snoozed Alerts that have reached SnoozeUntil date
        var snoozedExpiredAlerts = await alertRepository.GetSnoozedExpiredAsync(cancellationToken);
        foreach (var alert in snoozedExpiredAlerts)
        {
            var unsnoozeResult = alert.Unsnooze();
            if (unsnoozeResult.IsSuccess)
            {
                unsnoozedCount++;
                await alertRepository.UpdateAsync(alert, cancellationToken);
            }
        }

        if (expiredCount > 0 || unsnoozedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Alert maintenance finished. Expired: {ExpiredCount}, Unsnoozed: {UnsnoozedCount}.", expiredCount, unsnoozedCount);
        }
        else
        {
            _logger.LogDebug("Alert maintenance finished. No pending actions found.");
        }
    }
}
