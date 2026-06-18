using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

namespace Trap_Intel.Infrastructure.Persistence.SeedData;

/// <summary>
/// Orchestrates database seeding using individual seeders.
/// Provides professional, maintainable seeding with proper ordering and error handling.
/// </summary>
public sealed class DatabaseSeederOrchestrator
{
    private readonly ApplicationDbContext _context;
    private readonly IEnumerable<ISeeder> _seeders;
    private readonly ILogger<DatabaseSeederOrchestrator> _logger;

    public DatabaseSeederOrchestrator(
        ApplicationDbContext context,
        IEnumerable<ISeeder> seeders,
        ILogger<DatabaseSeederOrchestrator> logger)
    {
        _context = context;
        _seeders = seeders.OrderBy(s => s.Order);
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with all registered seeders in order.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("????????????????????????????????????????????????????????????????");
        _logger.LogInformation("?           Starting Database Seeding Process                  ?");
        _logger.LogInformation("????????????????????????????????????????????????????????????????");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var seededCount = 0;
        var skippedCount = 0;
        var errors = new List<(string Entity, Exception Error)>();

        foreach (var seeder in _seeders)
        {
            try
            {
                _logger.LogInformation("???????????????????????????????????????????????????????????????");
                _logger.LogInformation("? Processing: {EntityName} (Order: {Order})", 
                    seeder.EntityName.PadRight(30), seeder.Order);
                _logger.LogInformation("???????????????????????????????????????????????????????????????");

                var shouldSeed = await seeder.ShouldSeedAsync(_context, cancellationToken);
                
                if (shouldSeed)
                {
                    await seeder.SeedAsync(_context, cancellationToken);
                    seededCount++;
                    _logger.LogInformation("? {EntityName} seeded successfully", seeder.EntityName);
                }
                else
                {
                    skippedCount++;
                    _logger.LogInformation("? {EntityName} skipped (data exists)", seeder.EntityName);
                }
            }
            catch (Exception ex)
            {
                errors.Add((seeder.EntityName, ex));
                _logger.LogError(ex, "? Failed to seed {EntityName}", seeder.EntityName);
                
                // Continue with other seeders unless it's a critical dependency
                if (seeder.Order <= 4) // Plans, Organizations, Users, Subscriptions are critical
                {
                    throw new InvalidOperationException(
                        $"Critical seeder '{seeder.EntityName}' failed. Cannot continue.", ex);
                }
            }
        }

        stopwatch.Stop();

        _logger.LogInformation("????????????????????????????????????????????????????????????????");
        _logger.LogInformation("?           Database Seeding Complete                          ?");
        _logger.LogInformation("????????????????????????????????????????????????????????????????");
        _logger.LogInformation("? Seeded:  {Seeded}                                            ?", seededCount.ToString().PadRight(5));
        _logger.LogInformation("? Skipped: {Skipped}                                            ?", skippedCount.ToString().PadRight(5));
        _logger.LogInformation("? Errors:  {Errors}                                            ?", errors.Count.ToString().PadRight(5));
        _logger.LogInformation("? Time:    {Time}ms                                        ?", stopwatch.ElapsedMilliseconds.ToString().PadRight(8));
        _logger.LogInformation("????????????????????????????????????????????????????????????????");

        if (errors.Any())
        {
            _logger.LogWarning("Seeding completed with {ErrorCount} errors:", errors.Count);
            foreach (var (entity, error) in errors)
            {
                _logger.LogWarning("  - {Entity}: {Message}", entity, error.Message);
            }
        }
    }

    /// <summary>
    /// Seeds only specific entities by name.
    /// </summary>
    public async Task SeedSpecificAsync(IEnumerable<string> entityNames, CancellationToken cancellationToken = default)
    {
        var targetSeeders = _seeders
            .Where(s => entityNames.Contains(s.EntityName, StringComparer.OrdinalIgnoreCase))
            .OrderBy(s => s.Order);

        foreach (var seeder in targetSeeders)
        {
            _logger.LogInformation("Seeding specific entity: {EntityName}", seeder.EntityName);
            await seeder.SeedAsync(_context, cancellationToken);
        }
    }

    /// <summary>
    /// Checks if the database needs seeding (no data exists).
    /// </summary>
    public async Task<bool> NeedsSeedingAsync(CancellationToken cancellationToken = default)
    {
        // Check if the most fundamental entity (Plans) has data
        return !await _context.Plans.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Gets seeding status report.
    /// </summary>
    public async Task<SeedingStatusReport> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var report = new SeedingStatusReport();

        foreach (var seeder in _seeders)
        {
            var needsSeeding = await seeder.ShouldSeedAsync(_context, cancellationToken);
            report.EntityStatuses.Add(new EntitySeedStatus
            {
                EntityName = seeder.EntityName,
                Order = seeder.Order,
                NeedsSeeding = needsSeeding
            });
        }

        return report;
    }
}

/// <summary>
/// Seeding status report
/// </summary>
public sealed class SeedingStatusReport
{
    public List<EntitySeedStatus> EntityStatuses { get; } = [];
    public bool NeedsAnySeeding => EntityStatuses.Any(e => e.NeedsSeeding);
    public int TotalEntities => EntityStatuses.Count;
    public int EntitiesNeedingSeeding => EntityStatuses.Count(e => e.NeedsSeeding);
}

/// <summary>
/// Individual entity seed status
/// </summary>
public sealed class EntitySeedStatus
{
    public required string EntityName { get; init; }
    public int Order { get; init; }
    public bool NeedsSeeding { get; init; }
}
