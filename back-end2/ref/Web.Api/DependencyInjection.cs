using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Web.Api.Infrastructure;

namespace Web.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // REMARK: If you want to use Controllers, you'll need this.
        services.AddControllers();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddRateLimiting();
        services.AddCorsPolicy();

        return services;
    }

    /// <summary>
    /// Configures rate limiting policies for the application.
    /// </summary>
    private static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Global rate limiter - applies to all endpoints
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Use IP address for partitioning
                string partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                });
            });

            // Authentication rate limiter - stricter limits for login/register
            options.AddPolicy(RateLimitPolicies.Authentication, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Password reset rate limiter - prevent email bombing
            options.AddPolicy(RateLimitPolicies.PasswordReset, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(15),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Token refresh rate limiter
            options.AddPolicy(RateLimitPolicies.TokenRefresh, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    }));

            // Standard API rate limiter for authenticated requests
            options.AddPolicy(RateLimitPolicies.StandardApi, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    }));

            // Customize rejection response
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";

                TimeSpan retryAfterValue = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter)
                    ? retryAfter
                    : TimeSpan.FromSeconds(60);

                var problemDetails = new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = 429,
                    detail = "Rate limit exceeded. Please try again later.",
                    instance = context.HttpContext.Request.Path.ToString(),
                    retryAfter = retryAfterValue.TotalSeconds
                };

                await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// Configures CORS policies for the application.
    /// </summary>
    private static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            // Development policy - permissive for local development
            options.AddPolicy(CorsPolicies.Development, policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });

            // Production policy - restrictive
            // Origins will be configured dynamically in Program.cs based on configuration
            options.AddPolicy(CorsPolicies.Production, policy =>
            {
                policy.WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                      .WithHeaders("Authorization", "Content-Type", "Accept", "X-Request-Id", "X-Correlation-Id")
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });

        return services;
    }
}

/// <summary>
/// Rate limiting policy names.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// Policy for authentication endpoints (login, register).
    /// Limit: 5 requests per minute.
    /// </summary>
    public const string Authentication = "auth";

    /// <summary>
    /// Policy for password reset endpoints.
    /// Limit: 3 requests per 15 minutes.
    /// </summary>
    public const string PasswordReset = "password-reset";

    /// <summary>
    /// Policy for token refresh endpoint.
    /// Limit: 10 requests per minute.
    /// </summary>
    public const string TokenRefresh = "token-refresh";

    /// <summary>
    /// Policy for standard authenticated API requests.
    /// Limit: 60 requests per minute.
    /// </summary>
    public const string StandardApi = "standard-api";
}

/// <summary>
/// CORS policy names.
/// </summary>
public static class CorsPolicies
{
    public const string Development = "development";
    public const string Production = "production";
}
