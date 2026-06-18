using System.Reflection;
using Application;
using HealthChecks.UI.Client;
using Infrastructure;
using Infrastructure.Notifications.RealTime;
using Infrastructure.Security;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Web.Api;
using Web.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddSwaggerGenWithAuth();

builder.Services
    .AddApplication()
    .AddPresentation()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

WebApplication app = builder.Build();

// SECURITY: Validate production security configuration at startup
// This will throw an exception if critical security settings are misconfigured
ProductionSecurityExtensions.ValidateProductionSecurityOrThrow(
    app.Environment,
    app.Configuration,
    app.Logger);

app.MapEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi();

    app.ApplyMigrations();

    // Seed database with mock data
    await app.SeedDatabaseAsync();

    // CORS: Use permissive policy in development
    app.UseCors(CorsPolicies.Development);
}
else
{
    // CORS: Use restrictive policy in production with configured origins
    string? corsOrigins = app.Configuration["Cors:AllowedOrigins"];
    if (!string.IsNullOrEmpty(corsOrigins))
    {
        string[] origins = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        app.UseCors(policy => policy
            .WithOrigins(origins)
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", "Accept", "X-Request-Id", "X-Correlation-Id")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
    }
    else
    {
        app.Logger.LogWarning("CORS origins not configured. Set 'Cors:AllowedOrigins' in configuration.");
        app.UseCors(CorsPolicies.Production);
    }
}

app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseRequestContextLogging();

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

// SECURITY: Apply rate limiting
app.UseRateLimiter();

// SECURITY: Apply production security middleware (HTTPS, HSTS)
app.UseProductionSecurity(app.Environment);

app.UseAuthentication();

app.UseAuthorization();

// REMARK: If you want to use Controllers, you'll need this.
app.MapControllers();

// Map SignalR hubs
app.MapHub<NotificationHub>("/hubs/notifications");

await app.RunAsync();

// REMARK: Required for functional and integration tests to work.
namespace Web.Api
{
    public partial class Program;
}
