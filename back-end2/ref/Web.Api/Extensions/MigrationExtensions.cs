using Infrastructure.Database;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Web.Api.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        // Apply ApplicationDbContext migrations (Domain entities)
        using ApplicationDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();

        // Apply IdentityDbContext migrations (ASP.NET Identity)
        using IdentityDbContext identityContext =
            scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        identityContext.Database.Migrate();
    }

    public static async Task SeedDatabaseAsync(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();
        IServiceProvider services = scope.ServiceProvider;
        ILogger<DatabaseSeeder> logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();

        try
        {
            DatabaseSeeder seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
        }
    }
}
