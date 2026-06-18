using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Infrastructure.Auditing.BackgroundServices;

/// <summary>
/// A background service that runs periodically to clean up expired audit logs.
/// This prevents the database from expanding infinitely while respecting the compliance retention period.
/// </summary>
internal sealed class AuditCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditCleanupBackgroundService> _logger;

    // Run cleanup every 24 hours
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24);

    public AuditCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AuditCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AuditCleanupBackgroundService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during audit cleanup process.");
            }

            // Wait for the next cycle
            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("AuditCleanupBackgroundService is stopping.");
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting scheduled audit log cleanup & archival.");

        using var scope = _serviceProvider.CreateScope();
        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditTrailRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Trap_Intel.Infrastructure.Persistence.ApplicationDbContext>();

        int archiveAfterDays = 90; // Default archive policy: 90 days
        var archivedCount = await auditRepository.ArchiveOlderThanAsync(archiveAfterDays);
        if (archivedCount > 0)
        {
            _logger.LogInformation("Archived {ArchivedCount} audit logs older than {Days} days.", archivedCount, archiveAfterDays);
        }

        var deletedCount = await auditRepository.DeleteExpiredEntriesAsync();
        if (deletedCount > 0)
        {
            _logger.LogInformation("Scheduled cleanup completed. Purged {DeletedCount} expired audit logs.", deletedCount);
        }

        if (archivedCount > 0 || deletedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            _logger.LogInformation("Scheduled cleanup completed. No active maintenance needed at this time.");
        }
    }
}
