using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData;

/// <summary>
/// Background service that handles database seeding on application startup.
/// Runs once and then completes.
/// </summary>
public sealed class DatabaseSeedingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeedingBackgroundService> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public DatabaseSeedingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseSeedingBackgroundService> logger,
        IHostApplicationLifetime lifetime)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the application to start
        await Task.Yield();

        _logger.LogInformation("Database seeding background service started");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Apply migrations first
            await ApplyMigrationsAsync(context, stoppingToken);

            // Then seed data
            var orchestrator = scope.ServiceProvider.GetRequiredService<DatabaseSeederOrchestrator>();
            
            if (await orchestrator.NeedsSeedingAsync(stoppingToken))
            {
                _logger.LogInformation("Database needs seeding, starting seeding process...");
                await orchestrator.SeedAsync(stoppingToken);
            }
            else
            {
                _logger.LogInformation("Database already contains data, skipping seeding");
            }

            _logger.LogInformation("Database seeding background service completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Database seeding failed! Application may not function correctly.");
            
            // Optionally stop the application on critical seeding failure
            // _lifetime.StopApplication();
            throw;
        }
    }

    private async Task ApplyMigrationsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for pending migrations...");

        try
        {
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
            var migrations = pendingMigrations.ToList();

            if (migrations.Any())
            {
                _logger.LogInformation("Applying {Count} pending migrations: {Migrations}", 
                    migrations.Count, string.Join(", ", migrations));
                
                await context.Database.MigrateAsync(cancellationToken);
                
                _logger.LogInformation("Migrations applied successfully");
            }
            else
            {
                _logger.LogInformation("No pending migrations");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply migrations, attempting to create database...");
            
            // If migrations fail, try to ensure database is created (for initial setup)
            await context.Database.EnsureCreatedAsync(cancellationToken);
            
            _logger.LogInformation("Database schema created successfully");
        }
    }
}
