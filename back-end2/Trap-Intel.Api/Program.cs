using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Trap_Intel.Api.Endpoints;
using Trap_Intel.Api.Endpoints.Roles;
using Trap_Intel.Application;
using Trap_Intel.Infrastructure.Extensions;
using Trap_Intel.Infrastructure.Persistence;
using Trap_Intel.Infrastructure.Persistence.SeedData;
using Trap_Intel.Api.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

// Add services to the container.
// Note: Controllers kept for backwards compatibility, prefer Minimal APIs for new endpoints
builder.Services.AddControllers();

// Register all modular IEndpoint implementations
builder.Services.AddEndpoints(typeof(Program).Assembly);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace('+', '.'));
});

// Add Infrastructure services (Database, Repositories, Authentication, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Application services (MediatR, AutoMapper, FluentValidation, etc.)
builder.Services.AddApplication();

// Add OpenTelemetry metrics (Prometheus scraping + optional OTLP export)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService(
            serviceName: "Trap-Intel.Api",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown");
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("TrapIntel.Infrastructure.Billing.BackgroundServices")
            .AddPrometheusExporter();

        var otlpEndpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            metrics.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);

                var protocolValue = builder.Configuration["OpenTelemetry:Otlp:Protocol"];
                if (Enum.TryParse<OtlpExportProtocol>(protocolValue, ignoreCase: true, out var protocol))
                {
                    options.Protocol = protocol;
                }
            });
        }
    });

// Add database seeding with background service option
// Set useBackgroundService: true to run seeding in background
builder.Services.AddDatabaseSeeding(useBackgroundService: false);

// Add Rate Limiting (Brute Force Protection)
builder.Services.AddRateLimiter(options =>
{
    // Global rate limiter
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Auth-specific rate limiter (stricter for login attempts)
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,          // 10 login attempts
                Window = TimeSpan.FromMinutes(5),  // per 5 minutes
                SegmentsPerWindow = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";
        
        var problem = new
        {
            type = "https://httpstatuses.com/429",
            title = "Too Many Requests",
            status = 429,
            detail = "Rate limit exceeded. Please try again later.",
            retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? retryAfter.TotalSeconds
                : 60
        };

        await context.HttpContext.Response.WriteAsJsonAsync(problem, token);
    };
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                ?? ["http://localhost:3000", "http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders(
                "X-Realtime-Entity",
                "X-Realtime-Scope",
                "X-Realtime-Filter-Key",
                "X-Realtime-Hub");
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

var app = builder.Build();

// Apply database migrations and seed data automatically in Docker/Development
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var hasMigrationHistory = await MigrationHistoryTableExistsAsync(dbContext, logger);

        if (hasMigrationHistory)
        {
            logger.LogInformation("Applying database migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            logger.LogWarning("Migration history table not found. Using EnsureCreated for schema bootstrap.");
            await dbContext.Database.EnsureCreatedAsync();
            logger.LogInformation("Database schema is ready.");
        }

        await NormalizeLegacyAuditTrailEnumsAsync(dbContext, logger);
        await NormalizeLegacyAlertEnumsAsync(dbContext, logger);
        await NormalizeLegacyHoneypotEnumsAsync(dbContext, logger);

        var orchestrator = scope.ServiceProvider.GetRequiredService<DatabaseSeederOrchestrator>();
        if (await orchestrator.NeedsSeedingAsync())
        {
            await orchestrator.SeedAsync();
        }
        else
        {
            logger.LogInformation("Database already contains data, skipping seeding.");
        }
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChangesWarning", StringComparison.Ordinal))
    {
        logger.LogWarning(ex, "Pending model changes detected. Falling back to EnsureCreated for this environment.");
        await dbContext.Database.EnsureCreatedAsync();

        await NormalizeLegacyAuditTrailEnumsAsync(dbContext, logger);
        await NormalizeLegacyAlertEnumsAsync(dbContext, logger);
        await NormalizeLegacyHoneypotEnumsAsync(dbContext, logger);

        var orchestrator = scope.ServiceProvider.GetRequiredService<DatabaseSeederOrchestrator>();
        if (await orchestrator.NeedsSeedingAsync())
        {
            await orchestrator.SeedAsync();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while bootstrapping the database.");
        throw;
    }
}

async Task<bool> MigrationHistoryTableExistsAsync(ApplicationDbContext dbContext, Microsoft.Extensions.Logging.ILogger<Program> logger)
{
    const string sql = """
SELECT EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = 'trapintel'
      AND table_name = '__ef_migrations_history'
) AS "Value"
""";

    try
    {
        return await dbContext.Database.SqlQueryRaw<bool>(sql).SingleAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not verify EF migration history table.");
        return false;
    }
}

async Task NormalizeLegacyAuditTrailEnumsAsync(
    ApplicationDbContext dbContext,
    Microsoft.Extensions.Logging.ILogger<Program> logger,
    CancellationToken cancellationToken = default)
{
    const string sql = """
UPDATE trapintel.audit_trails
SET
    action = CASE action
        WHEN 'Created' THEN 'Create'
        WHEN 'Updated' THEN 'Update'
        WHEN 'Deleted' THEN 'Delete'
        WHEN 'Login' THEN 'View'
        WHEN 'Logout' THEN 'View'
        WHEN 'Viewed' THEN 'View'
        WHEN 'Acknowledged' THEN 'Approve'
        WHEN 'ConfigurationChanged' THEN 'Update'
        WHEN 'Generated' THEN 'Export'
        WHEN 'Upgraded' THEN 'Update'
        WHEN 'Detected' THEN 'View'
        WHEN 'PermissionsChanged' THEN 'Update'
        WHEN 'SecurityAlert' THEN 'View'
        ELSE action
    END,
    resource_type = CASE resource_type
        WHEN 'Honeypot' THEN 'HoneyPot'
        WHEN 'Alert' THEN 'Recommendation'
        WHEN 'ApiKey' THEN 'Settings'
        WHEN 'ThreatActor' THEN 'Recommendation'
        WHEN 'Security' THEN 'Settings'
        ELSE resource_type
    END
WHERE action IN (
        'Created', 'Updated', 'Deleted', 'Login', 'Logout', 'Viewed',
        'Acknowledged', 'ConfigurationChanged', 'Generated', 'Upgraded',
        'Detected', 'PermissionsChanged', 'SecurityAlert'
    )
   OR resource_type IN ('Honeypot', 'Alert', 'ApiKey', 'ThreatActor', 'Security');
""";

    try
    {
        var rowsAffected = await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        if (rowsAffected > 0)
        {
            logger.LogInformation("Normalized {RowCount} legacy audit trail enum values.", rowsAffected);
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to normalize legacy audit trail enum values.");
    }
}

async Task NormalizeLegacyAlertEnumsAsync(
    ApplicationDbContext dbContext,
    Microsoft.Extensions.Logging.ILogger<Program> logger,
    CancellationToken cancellationToken = default)
{
    const string sql = """
UPDATE trapintel.alerts
SET alert_type = CASE alert_type
    WHEN 'LateralMovement' THEN 'APTActivity'
    WHEN 'DataExfiltration' THEN 'AnomalyDetected'
    WHEN 'RDPExploit' THEN 'HighSeverityAttack'
    ELSE alert_type
END
WHERE alert_type IN ('LateralMovement', 'DataExfiltration', 'RDPExploit');
""";

    try
    {
        var rowsAffected = await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        if (rowsAffected > 0)
        {
            logger.LogInformation("Normalized {RowCount} legacy alert enum values.", rowsAffected);
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to normalize legacy alert enum values.");
    }
}

async Task NormalizeLegacyHoneypotEnumsAsync(
    ApplicationDbContext dbContext,
    Microsoft.Extensions.Logging.ILogger<Program> logger,
    CancellationToken cancellationToken = default)
{
    const string sql = """
UPDATE trapintel.honeypots
SET config_capture_level = CASE config_capture_level
    WHEN 'Full' THEN 'Verbose'
    ELSE config_capture_level
END
WHERE config_capture_level IN ('Full');
""";

    try
    {
        var rowsAffected = await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        if (rowsAffected > 0)
        {
            logger.LogInformation("Normalized {RowCount} legacy honeypot enum values.", rowsAffected);
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to normalize legacy honeypot enum values.");
    }
}

// Configure the HTTP request pipeline.
var exposeApiDocs = app.Environment.IsDevelopment() ||
                    string.Equals(app.Environment.EnvironmentName, "Docker", StringComparison.OrdinalIgnoreCase);

if (exposeApiDocs)
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Trap-Intel API v1");
        options.RoutePrefix = "swagger";
    });
}

if (app.Environment.EnvironmentName != "Docker")
{
    app.UseHttpsRedirection();
}

// Serve static assets (notification demo client, docs, etc.)
app.UseDefaultFiles();
app.UseStaticFiles();

// CORS - must be before authentication
app.UseCors("AllowFrontend");

// Rate Limiting
app.UseRateLimiter();

// Structured HTTP request logging
app.UseSerilogRequestLogging();

// Audit Logging Interceptor Middleware (Records unhandled critical exceptions for Users)
app.UseMiddleware<Trap_Intel.Infrastructure.Auditing.Middlewares.AuditLoggingMiddleware>();

// Authentication & Authorization - Order matters!
app.UseAuthentication();
app.UseAuthorization();

// Map Minimal API endpoints (preferred for new endpoints)
// Rate limiting is configured inside the endpoint group
app.MapAdminEndpoints();

// Map all modular endpoints created via IEndpoint
app.MapEndpoints();

// Map SignalR Hub
app.MapHub<Trap_Intel.Infrastructure.Notifications.RealTime.NotificationHub>("/hubs/notifications");
app.MapHub<Trap_Intel.Infrastructure.Notifications.RealTime.ListUpdatesHub>("/hubs/lists");

// Map Controllers (kept for backwards compatibility)
app.MapControllers();

// Map health check endpoint
app.MapHealthChecks("/health");

// Map Prometheus metrics scraping endpoint
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

public partial class Program
{
}
