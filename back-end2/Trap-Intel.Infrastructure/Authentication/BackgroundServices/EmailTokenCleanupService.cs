using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Infrastructure.Authentication.Configuration;

namespace Trap_Intel.Infrastructure.Authentication.BackgroundServices;

/// <summary>
/// Background service that periodically cleans up expired email verification and password reset tokens.
/// </summary>
public sealed class EmailTokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<EmailTokenCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;
    private readonly TimeSpan _retentionPeriod;

    public EmailTokenCleanupService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<TokenCleanupSettings> settings,
        ILogger<EmailTokenCleanupService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        
        var config = settings.Value;
        _cleanupInterval = TimeSpan.FromHours(config.CleanupIntervalHours);
        _retentionPeriod = TimeSpan.FromDays(config.RetentionDays);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Email Token Cleanup Service starting. Interval: {Interval}, Retention: {Retention}",
            _cleanupInterval, _retentionPeriod);

        // Initial delay to allow application startup
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokensAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during email token cleanup");
            }

            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Email Token Cleanup Service stopping");
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var emailVerificationTokenRepository = scope.ServiceProvider.GetRequiredService<IEmailVerificationTokenRepository>();
        var passwordResetTokenRepository = scope.ServiceProvider.GetRequiredService<IPasswordResetTokenRepository>();

        var cutoffDate = DateTime.UtcNow - _retentionPeriod;

        // Cleanup email verification tokens
        var emailTokensDeleted = await emailVerificationTokenRepository.DeleteExpiredTokensAsync(cutoffDate, cancellationToken);
        if (emailTokensDeleted > 0)
        {
            _logger.LogInformation(
                "Deleted {Count} expired email verification tokens older than {CutoffDate}",
                emailTokensDeleted, cutoffDate);
        }

        // Cleanup password reset tokens
        var passwordTokensDeleted = await passwordResetTokenRepository.DeleteExpiredTokensAsync(cutoffDate, cancellationToken);
        if (passwordTokensDeleted > 0)
        {
            _logger.LogInformation(
                "Deleted {Count} expired password reset tokens older than {CutoffDate}",
                passwordTokensDeleted, cutoffDate);
        }
    }
}
