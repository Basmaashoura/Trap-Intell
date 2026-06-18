using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Security;

/// <summary>
/// Production security configuration helpers.
/// </summary>
public static class ProductionSecurityExtensions
{
    private static readonly string[] PlaceholderSecrets =
    [
        "CHANGE_ME",
        "CHANGE_ME_TO_STRONG_SECRET",
        "super-duper-secret",
        "your-secret-here",
        "placeholder"
    ];

    /// <summary>
    /// Configures production security settings.
    /// This should be called in Program.cs for production deployments.
    /// </summary>
    public static IApplicationBuilder UseProductionSecurity(this IApplicationBuilder app, IHostEnvironment env)
    {
        if (env.IsProduction())
        {
            // Enforce HTTPS redirection
            app.UseHttpsRedirection();

            // Add HSTS (HTTP Strict Transport Security)
            app.UseHsts();
        }

        return app;
    }

    /// <summary>
    /// Validates production security requirements at startup.
    /// Throws an exception if critical security settings are misconfigured.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when critical security settings are invalid.</exception>
    public static void ValidateProductionSecurityOrThrow(IHostEnvironment env, IConfiguration configuration, ILogger logger)
    {
        if (!env.IsProduction())
        {
            logger.LogInformation("Skipping production security validation in {Environment} environment.", env.EnvironmentName);
            return;
        }

        var criticalErrors = new List<string>();

        // CRITICAL: Check JWT secret - must not be placeholder
        string? jwtSecret = configuration["Jwt:Secret"];
        if (string.IsNullOrEmpty(jwtSecret))
        {
            criticalErrors.Add("JWT Secret is not configured. Set 'Jwt:Secret' via environment variable or secure configuration.");
        }
        else if (jwtSecret.Length < 32)
        {
            criticalErrors.Add($"JWT Secret is too short ({jwtSecret.Length} chars). Minimum 32 characters required for security.");
        }
        else if (IsPlaceholderSecret(jwtSecret))
        {
            criticalErrors.Add("JWT Secret contains a placeholder value. Configure a strong, unique secret for production.");
        }

        // CRITICAL: Check database password - must not be placeholder
        string? connectionString = configuration.GetConnectionString("Database");
        if (string.IsNullOrEmpty(connectionString))
        {
            criticalErrors.Add("Database connection string is not configured.");
        }
        else if (ContainsPlaceholderPassword(connectionString))
        {
            criticalErrors.Add("Database connection string contains a placeholder password. Configure proper credentials for production.");
        }

        // CRITICAL: Fail startup if any critical errors
        if (criticalErrors.Count > 0)
        {
            foreach (string error in criticalErrors)
            {
                logger.LogCritical("SECURITY CONFIGURATION ERROR: {Error}", error);
            }

            throw new InvalidOperationException(
                $"Application startup aborted due to {criticalErrors.Count} critical security configuration error(s). " +
                "Check logs for details and configure proper secrets before deploying to production.");
        }

        // WARNINGS: Non-critical but recommended
        string? httpsPort = configuration["ASPNETCORE_HTTPS_PORT"];
        if (string.IsNullOrEmpty(httpsPort))
        {
            logger.LogWarning(
                "SECURITY WARNING: HTTPS port not explicitly configured. " +
                "Ensure HTTPS is enforced at the load balancer or reverse proxy level.");
        }

        if (connectionString?.Contains("Password=", StringComparison.OrdinalIgnoreCase) == true)
        {
            logger.LogWarning(
                "SECURITY WARNING: Database connection string contains inline password. " +
                "Consider using environment variables, Azure Key Vault, or managed identity.");
        }

        logger.LogInformation("Production security validation completed successfully.");
    }

    private static bool IsPlaceholderSecret(string secret)
    {
        return PlaceholderSecrets.Any(placeholder =>
            secret.Contains(placeholder, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsPlaceholderPassword(string connectionString)
    {
        // Extract password from connection string
        int passwordStart = connectionString.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
        if (passwordStart < 0)
        {
            return false;
        }

        int valueStart = passwordStart + "Password=".Length;
        int valueEnd = connectionString.IndexOf(';', valueStart);
        string password = valueEnd > 0
            ? connectionString[valueStart..valueEnd]
            : connectionString[valueStart..];

        return PlaceholderSecrets.Any(placeholder =>
            password.Contains(placeholder, StringComparison.OrdinalIgnoreCase));
    }
}
