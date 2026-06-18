using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trap_Intel.Infrastructure.Authentication.Services;

namespace Trap_Intel.Infrastructure.Authentication.BackgroundServices;

/// <summary>
/// Background service that periodically cleans up expired refresh tokens.
/// Runs daily to remove tokens that have been expired for longer than the retention period.
/// </summary>
public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Run once daily

    public RefreshTokenCleanupService(
        IServiceProvider serviceProvider,
        ILogger<RefreshTokenCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RefreshToken Cleanup Service starting");

        // Initial delay to let the application start up
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokensAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during refresh token cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("RefreshToken Cleanup Service stopping");
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Starting refresh token cleanup");

        using var scope = _serviceProvider.CreateScope();
        var refreshTokenService = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

        var deletedCount = await refreshTokenService.CleanupExpiredTokensAsync(stoppingToken);

        if (deletedCount > 0)
        {
            _logger.LogInformation("Refresh token cleanup completed. Deleted {Count} expired tokens", deletedCount);
        }
        else
        {
            _logger.LogDebug("Refresh token cleanup completed. No expired tokens to delete");
        }
    }
}
