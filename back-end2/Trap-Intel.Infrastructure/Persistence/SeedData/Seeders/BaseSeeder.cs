using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Base class for all seeders providing common functionality
/// </summary>
public abstract class BaseSeeder : ISeeder
{
    protected readonly ILogger Logger;

    protected BaseSeeder(ILogger logger)
    {
        Logger = logger;
    }

    public abstract int Order { get; }
    public abstract string EntityName { get; }

    public abstract Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default);
    public abstract Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes raw SQL safely, escaping curly braces to prevent format string issues
    /// </summary>
    protected async Task ExecuteSqlAsync(ApplicationDbContext context, string sql, CancellationToken cancellationToken)
    {
        // Note: Using ExecuteSqlRawAsync requires escaping {} as {{}} for JSON values
        await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    protected void LogSeeding(string message)
    {
        Logger.LogInformation("[{EntityName}] {Message}", EntityName, message);
    }

    protected void LogSeeded(int count)
    {
        Logger.LogInformation("[{EntityName}] Successfully seeded {Count} records", EntityName, count);
    }

    protected void LogSkipped(string reason)
    {
        Logger.LogInformation("[{EntityName}] Skipped seeding: {Reason}", EntityName, reason);
    }

    protected void LogError(Exception ex, string message)
    {
        Logger.LogError(ex, "[{EntityName}] {Message}", EntityName, message);
    }
}
