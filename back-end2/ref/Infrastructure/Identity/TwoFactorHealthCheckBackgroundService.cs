using Application.Abstractions.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity;

/// <summary>
/// Background service for periodic 2FA health checks.
/// Monitors the health of the two-factor authentication system.
/// </summary>
internal sealed class TwoFactorHealthCheckBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TwoFactorHealthCheckBackgroundService> _logger;
    private readonly TwoFactorHealthCheckSettings _settings;

    public TwoFactorHealthCheckBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TwoFactorHealthCheckBackgroundService> logger,
        IOptions<TwoFactorHealthCheckSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("2FA health check background service is disabled");
            return;
        }

        _logger.LogInformation(
            "2FA health check background service started. Interval: {Interval} minutes",
            _settings.IntervalMinutes);

        // Wait a bit before first run to let the app fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        // Perform initial health check
        await PerformHealthCheckAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for next check interval
                await Task.Delay(TimeSpan.FromMinutes(_settings.IntervalMinutes), stoppingToken);
                
                await PerformHealthCheckAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during 2FA health check. Will retry at next interval.");
            }
        }

        _logger.LogInformation("2FA health check background service stopped");
    }

    private async Task PerformHealthCheckAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ITwoFactorHealthCheckService healthCheckService = 
            scope.ServiceProvider.GetRequiredService<ITwoFactorHealthCheckService>();

        TwoFactorHealthCheckResult result = await healthCheckService.PerformHealthCheckAsync(cancellationToken);

        if (result.IsHealthy)
        {
            _logger.LogInformation(
                "2FA System Health: HEALTHY | " +
                "Users with 2FA: {EnabledCount} | " +
                "Pending setup: {PendingCount} | " +
                "Provider: Configured | " +
                "TOTP: Working",
                result.UsersWithTwoFactorEnabled,
                result.UsersWithTwoFactorPending);
        }
        else
        {
            _logger.LogError(
                "2FA System Health: UNHEALTHY | Error: {Error}",
                result.ErrorMessage);
        }
    }
}
