using Application.Abstractions.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity;

/// <summary>
/// Background service for periodic token cleanup.
/// Follows OCP - cleanup interval is configurable via settings.
/// </summary>
internal sealed class TokenCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TokenCleanupBackgroundService> _logger;
    private readonly TokenCleanupSettings _settings;

    public TokenCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TokenCleanupBackgroundService> logger,
        IOptions<TokenCleanupSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Token cleanup background service is disabled");
            return;
        }

        _logger.LogInformation(
            "Token cleanup background service started. Interval: {Interval} minutes",
            _settings.IntervalMinutes);

        // Wait a bit before first run to let the app fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token cleanup. Will retry at next interval.");
            }

            // Wait for next cleanup interval
            await Task.Delay(TimeSpan.FromMinutes(_settings.IntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Token cleanup background service stopped");
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ITokenCleanupService cleanupService = scope.ServiceProvider.GetRequiredService<ITokenCleanupService>();

        int cleanedUp = await cleanupService.PerformFullCleanupAsync(cancellationToken);

        if (cleanedUp > 0)
        {
            _logger.LogInformation("Background cleanup removed {Count} expired tokens", cleanedUp);
        }
    }
}
