using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Infrastructure.Authorization;
using Trap_Intel.Infrastructure.Persistence;
using Trap_Intel.Infrastructure.Persistence.SeedData;
using Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;
using Trap_Intel.Infrastructure.Authentication.Configuration;
using Trap_Intel.Infrastructure.Authentication.Services;
using Trap_Intel.Infrastructure.Authentication.Repositories;
using Trap_Intel.Infrastructure.Authentication.BackgroundServices;

using Microsoft.AspNetCore.Identity;
using Trap_Intel.Infrastructure.Authentication.Identity;
using Trap_Intel.Application.Abstractions.Identity;
using Trap_Intel.Application.Abstractions.Media;
using Trap_Intel.Application.Abstractions.Billing;
using Trap_Intel.Infrastructure.Configuration;
using Trap_Intel.Infrastructure.Media;
using Trap_Intel.Infrastructure.Billing.Pdf;
using Trap_Intel.Infrastructure.Billing.Services;
using Trap_Intel.Infrastructure.Billing.BackgroundServices;
using Trap_Intel.Application.Plans.Configuration;

namespace Trap_Intel.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddRepositories();
        services.AddExternalServices(configuration);
        services.AddBackgroundServices();
        services.AddCaching(configuration);
        services.AddMessaging(configuration);
        services.AddAuthentication(configuration);
        services.AddAuthorizationPolicies();

        return services;
    }

    /// <summary>
    /// Adds database seeding services with all seeders registered in proper order.
    /// </summary>
    public static IServiceCollection AddDatabaseSeeding(this IServiceCollection services, bool useBackgroundService = false)
    {
        // Register individual seeders (Order 1-10: Core entities)
        services.AddScoped<ISeeder, PlanSeeder>();
        services.AddScoped<ISeeder, OrganizationSeeder>();
        services.AddScoped<ISeeder, RoleSeeder>();
        services.AddScoped<ISeeder, UserSeeder>();
        services.AddScoped<ISeeder, SubscriptionSeeder>();
        services.AddScoped<ISeeder, HoneypotSeeder>();
        services.AddScoped<ISeeder, ThreatActorSeeder>();
        services.AddScoped<ISeeder, AttackEventSeeder>();
        services.AddScoped<ISeeder, AlertSeeder>();
        services.AddScoped<ISeeder, ApiKeySeeder>();
        services.AddScoped<ISeeder, WebhookSeeder>();
        
        // Register additional seeders (Order 11-17: Extended entities)
        services.AddScoped<ISeeder, SubscriptionQuotaSeeder>();
        services.AddScoped<ISeeder, PaymentMethodSeeder>();
        services.AddScoped<ISeeder, InvoiceSeeder>();
        services.AddScoped<ISeeder, DashboardViewSeeder>();
        services.AddScoped<ISeeder, AgentCommandSeeder>();
        services.AddScoped<ISeeder, AIRecommendationSeeder>();
        services.AddScoped<ISeeder, AuditTrailSeeder>();

        // Register orchestrator
        services.AddScoped<DatabaseSeederOrchestrator>();

        // Legacy seeder for backward compatibility
        services.AddScoped<DatabaseSeeder>();

        // Optionally add background service
        if (useBackgroundService)
        {
            services.AddHostedService<DatabaseSeedingBackgroundService>();
        }

        return services;
    }

    private static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddScoped<Trap_Intel.Infrastructure.Auditing.Interceptors.AuditingSaveInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var auditInterceptor = sp.GetRequiredService<Trap_Intel.Infrastructure.Auditing.Interceptors.AuditingSaveInterceptor>();

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "trapintel");
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });

            options.AddInterceptors(auditInterceptor);

            // Enable sensitive data logging in development
            #if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            #endif
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Register repositories here
        services.AddScoped<Trap_Intel.Domain.Organizations.IOrganizationRepository, Trap_Intel.Infrastructure.Organizations.OrganizationRepository>();
        services.AddScoped<Trap_Intel.Domain.Identity.IUserRepository, Trap_Intel.Infrastructure.Authentication.Repositories.UserRepository>();
        services.AddScoped<Trap_Intel.Domain.Honeypots.IHoneypotRepository, Trap_Intel.Infrastructure.Honeypots.HoneypotRepository>();
        services.AddScoped<Trap_Intel.Domain.Subscriptions.ISubscriptionRepository, Trap_Intel.Infrastructure.Subscriptions.SubscriptionRepository>();
        services.AddScoped<Trap_Intel.Domain.Billing.IInvoiceRepository, Trap_Intel.Infrastructure.Billing.InvoiceRepository>();
        services.AddScoped<Trap_Intel.Domain.Billing.IInvoiceNumberGenerator, SequentialInvoiceNumberGenerator>();
        services.AddScoped<Trap_Intel.Domain.Attacks.IAttackEventRepository, Trap_Intel.Infrastructure.Attacks.AttackEventRepository>();
        services.AddScoped<Trap_Intel.Domain.Alerts.IAlertRepository, Trap_Intel.Infrastructure.Alerts.Repositories.AlertRepository>();
        // services.AddScoped<IThreatActorRepository, ThreatActorRepository>();
        // services.AddScoped<IAgentCommandRepository, AgentCommandRepository>();
        services.AddScoped<Trap_Intel.Domain.Invitations.IOrganizationInvitationRepository, Trap_Intel.Infrastructure.Invitations.OrganizationInvitationRepository>();
        // services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        // services.AddScoped<IWebhookRepository, WebhookRepository>();
        services.AddScoped<Trap_Intel.Domain.Auditing.IAuditTrailRepository, Trap_Intel.Infrastructure.Auditing.Repositories.AuditTrailRepository>();
        services.AddScoped<Trap_Intel.Application.Abstractions.Auditing.IAuditService, Trap_Intel.Infrastructure.Auditing.Services.AuditService>();
        services.AddScoped<Trap_Intel.Application.Abstractions.Auditing.IAuditDashboardQueryService, Trap_Intel.Infrastructure.Auditing.Services.AuditDashboardQueryService>();
        services.AddScoped<Trap_Intel.Application.Abstractions.Auditing.IAuditExportService, Trap_Intel.Infrastructure.Auditing.Services.AuditExportService>();
        services.AddScoped<Trap_Intel.Application.Abstractions.Alerts.IAlertQueryService, Trap_Intel.Infrastructure.Alerts.Services.AlertQueryService>();
        // services.AddScoped<IAIRecommendationRepository, AIRecommendationRepository>();
        services.AddScoped<Trap_Intel.Domain.Plans.IPlanRepository, Trap_Intel.Infrastructure.Plans.PlanRepository>();
        services.AddScoped<Trap_Intel.Domain.Billing.IPaymentMethodRepository, Trap_Intel.Infrastructure.Billing.PaymentMethodRepository>();
        // services.AddScoped<IDashboardViewRepository, DashboardViewRepository>();

        return services;
    }

    private static IServiceCollection AddExternalServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register external services here
        // services.AddScoped<IEmailService, EmailService>();
        // services.AddScoped<IExternalHoneypotService, ExternalHoneypotService>();
        services.Configure<PaymentGatewaySettings>(configuration.GetSection(PaymentGatewaySettings.SectionName));
        services.Configure<PlanLifecycleNotificationOptions>(
            configuration.GetSection(PlanLifecycleNotificationOptions.SectionName));

        services.AddHttpClient<Trap_Intel.Infrastructure.Billing.StripePaymentProcessor>((serviceProvider, client) =>
        {
            var paymentGatewaySettings = serviceProvider.GetRequiredService<IOptions<PaymentGatewaySettings>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(paymentGatewaySettings.StripeApiBaseUrl)
                ? "https://api.stripe.com/v1/"
                : paymentGatewaySettings.StripeApiBaseUrl;

            client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<Trap_Intel.Infrastructure.Billing.UnconfiguredPaymentProcessor>();

        services.AddScoped<Trap_Intel.Domain.Billing.IPaymentProcessor>(serviceProvider =>
        {
            var paymentGatewaySettings = serviceProvider.GetRequiredService<IOptions<PaymentGatewaySettings>>().Value;

            return paymentGatewaySettings.IsStripeConfigured
                ? serviceProvider.GetRequiredService<Trap_Intel.Infrastructure.Billing.StripePaymentProcessor>()
                : serviceProvider.GetRequiredService<Trap_Intel.Infrastructure.Billing.UnconfiguredPaymentProcessor>();
        });

        services.Configure<CloudinarySettings>(configuration.GetSection(CloudinarySettings.SectionName));

        services.AddScoped<CloudinaryMediaStorageService>();
        services.AddScoped<LocalMediaStorageService>();
        services.AddScoped<IMediaStorageService>(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<CloudinarySettings>>().Value;

            return IsCloudinaryConfigured(settings)
                ? serviceProvider.GetRequiredService<CloudinaryMediaStorageService>()
                : serviceProvider.GetRequiredService<LocalMediaStorageService>();
        });

            services.AddScoped<IInvoicePdfRenderer, QuestPdfInvoicePdfRenderer>();

        return services;
    }

    private static bool IsCloudinaryConfigured(CloudinarySettings settings)
    {
        return !string.IsNullOrWhiteSpace(settings.CloudName)
            && !string.IsNullOrWhiteSpace(settings.ApiKey)
            && !string.IsNullOrWhiteSpace(settings.ApiSecret);
    }

    private static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        // Register background services here
        // Register existing Background Services
        services.AddHostedService<RefreshTokenCleanupService>();
        services.AddHostedService<EmailTokenCleanupService>();
        services.AddHostedService<Trap_Intel.Infrastructure.Auditing.BackgroundServices.AuditCleanupBackgroundService>();
        services.AddHostedService<Trap_Intel.Infrastructure.Alerts.BackgroundServices.AlertMaintenanceBackgroundService>();
        services.AddHostedService<MonthlyInvoiceGenerationBackgroundService>();
        services.AddHostedService<OverdueInvoiceProcessingBackgroundService>();
        // services.AddHostedService<HoneypotHealthMonitorBackgroundService>();
        // services.AddHostedService<AttackEventProcessingBackgroundService>();
        // services.AddHostedService<SubscriptionRenewalBackgroundService>();
        // services.AddHostedService<AlertEscalationBackgroundService>();
        // services.AddHostedService<QuotaMonitoringBackgroundService>();

        return services;
    }

    private static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add caching services
        services.AddMemoryCache();
        
        // Uncomment to enable Redis distributed cache
        // var redisConnection = configuration.GetConnectionString("Redis");
        // if (!string.IsNullOrEmpty(redisConnection))
        // {
        //     services.AddStackExchangeRedisCache(options =>
        //     {
        //         options.Configuration = redisConnection;
        //         options.InstanceName = "TrapIntel:";
        //     });
        // }

        return services;
    }

    private static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add messaging services (RabbitMQ, Azure Service Bus, etc.)
        // This will be configured based on your message broker choice
        return services;
    }

    private static IServiceCollection AddAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Microsoft Identity configured strictly for Infrastructure and DbContext operations
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
        
        services.AddScoped<IPasswordHasher<ApplicationUser>, BCryptPasswordHasher>();

        // Register our custom port adapter that bridges MS Identity API and Application Domain Users
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IJwtProvider, JwtProvider>();

        // Configure JWT Settings
        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        services.Configure<JwtSettings>(jwtSection);

        // Configure Lockout Settings
        var lockoutSection = configuration.GetSection(LockoutSettings.SectionName);
        services.Configure<LockoutSettings>(lockoutSection);

        // Configure Refresh Token Settings
        var refreshTokenSection = configuration.GetSection(RefreshTokenSettings.SectionName);
        services.Configure<RefreshTokenSettings>(refreshTokenSection);

        // Configure Email Settings
        var emailSection = configuration.GetSection(EmailSettings.SectionName);
        services.Configure<EmailSettings>(emailSection);

        // Configure Email Verification Settings
        var emailVerificationSection = configuration.GetSection(EmailVerificationSettings.SectionName);
        services.Configure<EmailVerificationSettings>(emailVerificationSection);

        // Configure Password Reset Settings
        var passwordResetSection = configuration.GetSection(PasswordResetSettings.SectionName);
        services.Configure<PasswordResetSettings>(passwordResetSection);

        // Configure Token Cleanup Settings
        var tokenCleanupSection = configuration.GetSection(TokenCleanupSettings.SectionName);
        services.Configure<TokenCleanupSettings>(tokenCleanupSection);

        // Configure Two-Factor Authentication Settings
        var twoFactorSection = configuration.GetSection(TwoFactorSettings.SectionName);
        services.Configure<TwoFactorSettings>(twoFactorSection);

        // Register authentication services
        services.AddSingleton<IPasswordHashingService, PasswordHashingService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // Register refresh token services
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // Register email verification and password reset services
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IOrganizationEmailService, OrganizationEmailService>();
        services.AddScoped<IEmailTokenService, EmailTokenService>();

        // Register two-factor authentication services
        services.AddSingleton<ITwoFactorService, TwoFactorService>();
        services.AddScoped<ITwoFactorBackupCodeRepository, TwoFactorBackupCodeRepository>();
        services.AddScoped<ITwoFactorAuthService, TwoFactorAuthService>();

        // Configure JWT Bearer Authentication
        var jwtSettings = jwtSection.Get<JwtSettings>();
        if (jwtSettings != null && !string.IsNullOrEmpty(jwtSettings.SecretKey))
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Get token validation parameters from the service
                var serviceProvider = services.BuildServiceProvider();
                var jwtTokenService = serviceProvider.GetRequiredService<IJwtTokenService>();
                options.TokenValidationParameters = jwtTokenService.GetTokenValidationParameters();

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers["Token-Expired"] = "true";
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        }

        return services;
    }

    private static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        // Register authorization handlers
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, RoleHierarchyAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, SameOrganizationAuthorizationHandler>();

        // Register dynamic permission policy provider
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        services.AddScoped<Trap_Intel.Domain.Roles.IRoleRepository, Trap_Intel.Infrastructure.Roles.RoleRepository>();

        // IHttpContextAccessor needed by SameOrganizationAuthorizationHandler
        services.AddHttpContextAccessor();

        // Register named authorization policies
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.SuperAdminOnly, policy =>
                policy.AddRequirements(new RoleHierarchyRequirement(Trap_Intel.Domain.Roles.SystemRoles.SuperAdminId)))
            .AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
                policy.AddRequirements(new RoleHierarchyRequirement(Trap_Intel.Domain.Roles.SystemRoles.OrganizationAdminId)))
            .AddPolicy(AuthorizationPolicies.AnalystOrAbove, policy =>
                policy.AddRequirements(new RoleHierarchyRequirement(Trap_Intel.Domain.Roles.SystemRoles.OperationsAnalystId)))
            .AddPolicy(AuthorizationPolicies.ViewerOrAbove, policy =>
                policy.AddRequirements(new RoleHierarchyRequirement(Trap_Intel.Domain.Roles.SystemRoles.ViewerId)))
            .AddPolicy(AuthorizationPolicies.SameOrganization, policy =>
                policy.AddRequirements(new SameOrganizationRequirement()));

        // Configure Notifications Feature
        services.AddScoped<Trap_Intel.Domain.Notifications.INotificationRepository, Trap_Intel.Infrastructure.Notifications.Repositories.NotificationRepository>();
        services.AddScoped<Trap_Intel.Infrastructure.Notifications.Channels.INotificationChannel, Trap_Intel.Infrastructure.Notifications.Channels.EmailNotificationChannel>();
        services.AddScoped<Trap_Intel.Infrastructure.Notifications.Channels.INotificationChannel, Trap_Intel.Infrastructure.Notifications.Channels.PushNotificationChannel>();
        services.AddScoped<Trap_Intel.Infrastructure.Notifications.Channels.INotificationChannel, Trap_Intel.Infrastructure.Notifications.Channels.SmsNotificationChannel>();
        services.AddScoped<Trap_Intel.Infrastructure.Notifications.Channels.INotificationChannel, Trap_Intel.Infrastructure.Notifications.Channels.InAppNotificationChannel>();
        services.AddScoped<Trap_Intel.Application.Abstractions.Notifications.INotificationDispatcher, Trap_Intel.Infrastructure.Notifications.Services.NotificationDispatcher>();
        services.AddScoped<Trap_Intel.Application.Abstractions.RealTime.IListRealtimeNotifier, Trap_Intel.Infrastructure.Notifications.RealTime.SignalRListRealtimeNotifier>();

        // Register SignalR for Notifications Real-Time updates
        services.AddSignalR();

        return services;
    }
}

