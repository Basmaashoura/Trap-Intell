using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Alerts.Entities;
using Trap_Intel.Domain.ApiKeys;
using Trap_Intel.Domain.Attacks;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Commands;
using Trap_Intel.Domain.Dashboards;
using Trap_Intel.Domain.Honeypots;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Entities;
using Trap_Intel.Domain.Invitations;
using Trap_Intel.Domain.Organizations;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Recommendations;
using Trap_Intel.Domain.Reporting;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Domain.Subscriptions.Entities;
using Trap_Intel.Domain.ThreatActors;
using Trap_Intel.Domain.ThreatActors.Entities;
using Trap_Intel.Domain.Webhooks;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MediatR;
using Npgsql;
using Trap_Intel.Infrastructure.Authentication.Identity;

namespace Trap_Intel.Infrastructure.Persistence;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>, IUnitOfWork
{
    private const string DefaultPaymentMethodUniqueIndexName = "ux_payment_methods_org_default";

    private readonly IPublisher _publisher;
    private bool _isDispatchingDomainEvents;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IPublisher publisher)
        : base(options)
    {
        _publisher = publisher;
    }

    #region Organizations & Identity

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationAddress> OrganizationAddresses => Set<OrganizationAddress>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Trap_Intel.Domain.Roles.Role> Roles => Set<Trap_Intel.Domain.Roles.Role>();
    public DbSet<OrganizationInvitation> OrganizationInvitations => Set<OrganizationInvitation>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    #endregion

    #region Plans & Subscriptions

    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Domain.Subscriptions.Subscription> Subscriptions => Set<Domain.Subscriptions.Subscription>();
    public DbSet<SubscriptionQuotaEntity> SubscriptionQuotas => Set<SubscriptionQuotaEntity>();
    public DbSet<UsageSnapshot> UsageSnapshots => Set<UsageSnapshot>();
    public DbSet<MonthlyUsageSummary> MonthlyUsageSummaries => Set<MonthlyUsageSummary>();

    #endregion

    #region Billing

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Domain.Billing.PaymentMethod> PaymentMethods => Set<Domain.Billing.PaymentMethod>();

    #endregion

    #region Honeypots & Attacks

    public DbSet<Honeypot> Honeypots => Set<Honeypot>();
    public DbSet<AttackEvent> AttackEvents => Set<AttackEvent>();
    public DbSet<AgentCommand> AgentCommands => Set<AgentCommand>();

    #endregion

    #region Notifications
    public DbSet<Trap_Intel.Domain.Notifications.Notification> Notifications => Set<Trap_Intel.Domain.Notifications.Notification>();
    public DbSet<Trap_Intel.Domain.Notifications.UserPushToken> UserPushTokens => Set<Trap_Intel.Domain.Notifications.UserPushToken>();
    #endregion

    #region Alerts & Monitoring

    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AlertActionEntity> AlertActions => Set<AlertActionEntity>();
    public DbSet<AlertCommentEntity> AlertComments => Set<AlertCommentEntity>();
    public DbSet<AlertEscalationEntity> AlertEscalations => Set<AlertEscalationEntity>();
    public DbSet<AlertNotificationEntity> AlertNotifications => Set<AlertNotificationEntity>();

    #endregion

    #region Threat Intelligence

    public DbSet<ThreatActor> ThreatActors => Set<ThreatActor>();
    public DbSet<ThreatActorIPEntity> ThreatActorIPs => Set<ThreatActorIPEntity>();
    public DbSet<ThreatActorTTPEntity> ThreatActorTTPs => Set<ThreatActorTTPEntity>();
    public DbSet<BehaviorPatternEntity> BehaviorPatterns => Set<BehaviorPatternEntity>();
    public DbSet<ThreatIntelNoteEntity> ThreatIntelNotes => Set<ThreatIntelNoteEntity>();

    #endregion

    #region Integrations

    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Webhook> Webhooks => Set<Webhook>();

    #endregion

    #region Auditing & Reporting

    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();
    public DbSet<AIRecommendation> AIRecommendations => Set<AIRecommendation>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportTemplate> ReportTemplates => Set<ReportTemplate>();
    public DbSet<ReportExport> ReportExports => Set<ReportExport>();
    public DbSet<DashboardView> DashboardViews => Set<DashboardView>();

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure PostgreSQL-specific settings
        modelBuilder.HasDefaultSchema("trapintel");

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Update timestamps for entities
            UpdateTimestamps();

            var result = await base.SaveChangesAsync(cancellationToken);

            // Dispatch domain events after persistence succeeds.
            await DispatchDomainEventsAsync(cancellationToken);

            return result;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "The requested operation could not be completed because the resource was updated by another process.",
                exception);
        }
        catch (DbUpdateException exception) when (IsDefaultPaymentMethodConflict(exception))
        {
            throw new ConcurrencyConflictException(
                "The requested operation could not be completed because the organization default payment method was changed by another process.",
                exception);
        }
    }

    private static bool IsDefaultPaymentMethodConflict(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && string.Equals(postgresException.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal)
            && string.Equals(postgresException.ConstraintName, DefaultPaymentMethodUniqueIndexName, StringComparison.OrdinalIgnoreCase);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        if (_isDispatchingDomainEvents)
        {
            return;
        }

        _isDispatchingDomainEvents = true;

        try
        {
        var domainEntities = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(x => x.Entity)
            .Where(x => x.GetDomainEvents().Count > 0)
            .ToList();

        if (domainEntities.Count == 0)
        {
            return;
        }

        var domainEvents = domainEntities
            .SelectMany(x => x.GetDomainEvents())
            .ToList();

        // Clear first to avoid re-publishing the same events on nested SaveChanges calls.
        foreach (var entity in domainEntities)
        {
            entity.ClearDomainEvents();
        }

        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        }
        finally
        {
            _isDispatchingDomainEvents = false;
        }
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity.GetType().GetProperty("UpdatedAt") is { } prop)
            {
                prop.SetValue(entry.Entity, DateTime.UtcNow);
            }
        }
    }
}
