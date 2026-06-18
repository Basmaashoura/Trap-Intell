using System.Text;
using Application.Abstractions.Audit;
using Application.Abstractions.Data;
using Application.Abstractions.Email;
using Application.Abstractions.Identity;
using Application.Abstractions.Notifications;
using Application.Abstractions.Repositories;
using Infrastructure.Audit;
using Infrastructure.Authorization;
using Infrastructure.Database;
using Infrastructure.DomainEvents;
using Infrastructure.Email;
using Infrastructure.Identity;
using Infrastructure.Notifications;
using Infrastructure.Notifications.BackgroundServices;
using Infrastructure.Notifications.Channels;
using Infrastructure.Notifications.RealTime;
using Infrastructure.Notifications.Repositories;
using Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SharedKernel;

namespace Infrastructure;


public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddServices()
            .AddDatabase(configuration)
            .AddCaching()
            .AddIdentityServices(configuration)
            .AddEmailServices(configuration)
            .AddNotificationServices()
            .AddHealthChecks(configuration)
            .AddAuthenticationInternal(configuration)
            .AddAuthorizationInternal();

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();
        services.AddScoped<IAuditService, AuditService>();

        services.AddHttpContextAccessor();


        return services;
    }


    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("Database");

        // Application DbContext (Domain entities)
        services.AddDbContext<ApplicationDbContext>(
            options => options
                .UseNpgsql(connectionString, npgsqlOptions =>
                    npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Identity DbContext (ASP.NET Identity)
        services.AddDbContext<IdentityDbContext>(
            options => options
                .UseNpgsql(connectionString, npgsqlOptions =>
                    npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, IdentityDbContext.SchemaName))
                .UseSnakeCaseNamingConvention());

        // Database Seeder
        services.AddScoped<DatabaseSeeder>();

        return services;
    }

    private static IServiceCollection AddCaching(this IServiceCollection services)
    {
        // Memory cache for permission caching and other in-memory caching needs
        services.AddMemoryCache(options =>
        {
            // Set size limit to prevent unbounded memory growth
            options.SizeLimit = 1024; // Arbitrary units, managed by cache entry Size property
        });

        return services;
    }

    private static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Determine if we're in production
        bool isProduction = configuration["ASPNETCORE_ENVIRONMENT"] == "Production" ||
                           Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

        // Configure JWT settings
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Add ASP.NET Identity with secure defaults
        services.AddIdentity<ApplicationIdentityUser, ApplicationIdentityRole>(options =>
            {
                // Password settings - enforce strong passwords
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredUniqueChars = 4;

                // Lockout settings - protect against brute force
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;

                // SignIn settings - require email confirmation in production
                options.SignIn.RequireConfirmedEmail = isProduction;
                options.SignIn.RequireConfirmedAccount = isProduction;

                // Token settings - short-lived tokens for security
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultProvider;
            })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        // Configure token lifespan (20 minutes for password reset and email confirmation)
        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromMinutes(20);
        });

        // Register Identity services
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<JwtSecurityStampValidator>();

        // Register Token Cleanup services
        services.Configure<TokenCleanupSettings>(configuration.GetSection(TokenCleanupSettings.SectionName));
        services.AddScoped<ITokenCleanupService, TokenCleanupService>();
        services.AddHostedService<TokenCleanupBackgroundService>();

        // Register 2FA Health Check services
        services.Configure<TwoFactorHealthCheckSettings>(
            configuration.GetSection(TwoFactorHealthCheckSettings.SectionName));
        services.AddScoped<ITwoFactorHealthCheckService, TwoFactorHealthCheckService>();
        services.AddHostedService<TwoFactorHealthCheckBackgroundService>();

        return services;
    }

    private static IServiceCollection AddEmailServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind email settings from configuration
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        // Register email service
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }

    private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Database")!);

        return services;
    }

    private static IServiceCollection AddAuthenticationInternal(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Determine if we're in production based on configuration
        bool isProduction = configuration["ASPNETCORE_ENVIRONMENT"] == "Production" ||
                           Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // SECURITY: Require HTTPS in production
                options.RequireHttpsMetadata = isProduction;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Validate the signing key
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),

                    // Validate the issuer
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],

                    // Validate the audience
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],

                    // Validate token lifetime
                    ValidateLifetime = true,

                    // No clock skew tolerance - tokens expire exactly when they should
                    ClockSkew = TimeSpan.Zero
                };

                // Handle authentication events for logging/custom behavior
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // Log authentication failures for security monitoring
                        ILoggerFactory loggerFactory = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>();
                        ILogger logger = loggerFactory.CreateLogger("JwtAuthentication");

                        logger.LogWarning(
                            context.Exception,
                            "JWT authentication failed: {Message}",
                            context.Exception.Message);

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        // SECURITY: Validate SecurityStamp for instant token invalidation
                        // This enables immediate logout when:
                        // - User logs out
                        // - Password is changed
                        // - Roles/permissions are modified
                        // - Account is deactivated
                        JwtSecurityStampValidator? stampValidator = context.HttpContext.RequestServices
                            .GetService<JwtSecurityStampValidator>();

                        if (stampValidator is not null)
                        {
                            bool isValid = await stampValidator.ValidateSecurityStampAsync(context.Principal!);
                            if (!isValid)
                            {
                                context.Fail("Security stamp validation failed. Token has been invalidated.");
                                return;
                            }
                        }

                        // Additional validation: Check if user is still active
                        string? userIdClaim = context.Principal?.FindFirst(CustomClaimTypes.IdentityUserId)?.Value;
                        if (Guid.TryParse(userIdClaim, out Guid userId))
                        {
                            UserManager<ApplicationIdentityUser>? userManager = context.HttpContext.RequestServices
                                .GetService<UserManager<ApplicationIdentityUser>>();

                            if (userManager is not null)
                            {
                                ApplicationIdentityUser? user = await userManager.FindByIdAsync(userId.ToString());
                                if (user is null || !user.IsActive)
                                {
                                    context.Fail("User account is not active.");
                                }
                            }
                        }
                    }
                };
            });

        services.AddHttpContextAccessor();

        return services;
    }

    private static IServiceCollection AddAuthorizationInternal(this IServiceCollection services)
    {
        services.AddAuthorization();

        // Permission provider with database access and caching
        services.AddScoped<PermissionProvider>();

        // Permission-based authorization handler
        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // Dynamic policy provider for permission-based policies
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        return services;
    }

    private static IServiceCollection AddNotificationServices(this IServiceCollection services)
    {
        // SignalR
        services.AddSignalR();

        // Connection manager (singleton to track all connections)
        services.AddSingleton<UserConnectionManager>();

        // Repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationTypeRepository, NotificationTypeRepository>();
        services.AddScoped<IUserNotificationPreferencesRepository, UserNotificationPreferencesRepository>();
        services.AddScoped<IUserPushTokenRepository, UserPushTokenRepository>();

        // Real-time notification service
        services.AddScoped<IRealtimeNotificationService, RealtimeNotificationService>();

        // Notification services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        // Notification channels
        services.AddScoped<INotificationChannel, InAppNotificationChannel>();
        services.AddScoped<INotificationChannel, EmailNotificationChannel>();
        services.AddScoped<INotificationChannel, PushNotificationChannel>();

        // Background services
        services.AddHostedService<NotificationCleanupService>();
        services.AddHostedService<ScheduledNotificationService>();

        return services;
    }
}

