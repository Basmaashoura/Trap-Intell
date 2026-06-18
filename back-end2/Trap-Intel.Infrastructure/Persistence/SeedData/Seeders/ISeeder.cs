namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Interface for all database seeders
/// </summary>
public interface ISeeder
{
    /// <summary>
    /// Gets the order in which this seeder should be executed.
    /// Lower values execute first.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Gets the name of the entity being seeded.
    /// </summary>
    string EntityName { get; }

    /// <summary>
    /// Seeds the data asynchronously.
    /// </summary>
    Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if this seeder should run (i.e., data doesn't already exist).
    /// </summary>
    Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default);
}
