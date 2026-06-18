using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using Trap_Intel.Application.Abstractions.Auditing;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Domain.Subscriptions.Events;

namespace Trap_Intel.Application.Subscriptions.Events;

internal sealed class SubscriptionQuotaDomainEventAuditHandler :
    INotificationHandler<QuotaCreatedEvent>,
    INotificationHandler<QuotaChangedEvent>,
    INotificationHandler<QuotaWarningEvent>,
    INotificationHandler<QuotaExceededEvent>,
    INotificationHandler<QuotaEnforcementBlockedEvent>,
    INotificationHandler<MonthlyUsageFinalizedEvent>,
    INotificationHandler<MonthlyUsageBilledEvent>
{
    private const int MaxAuditReasonLength = 1000;
    private const int MaxAuditFieldLength = 160;

    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<SubscriptionQuotaDomainEventAuditHandler> _logger;

    public SubscriptionQuotaDomainEventAuditHandler(
        ISubscriptionRepository subscriptionRepository,
        IAuditService auditService,
        ILogger<SubscriptionQuotaDomainEventAuditHandler> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _auditService = auditService;
        _logger = logger;
    }

    public Task Handle(QuotaCreatedEvent notification, CancellationToken cancellationToken)
    {
        return RecordForSubscriptionAsync(
            notification.SubscriptionId,
            AuditAction.Create,
            AuditSeverity.Info,
            $"Subscription quota initialized. Honeypots={notification.MaxHoneypots}, StorageGb={notification.MaxStorageGb.ToString("0.####", CultureInfo.InvariantCulture)}, SourcePlan={SanitizeAuditValue(notification.SourcePlanId?.ToString(), MaxAuditFieldLength)}.",
            cancellationToken);
    }

    public Task Handle(QuotaChangedEvent notification, CancellationToken cancellationToken)
    {
        return RecordForSubscriptionAsync(
            notification.SubscriptionId,
            AuditAction.Update,
            AuditSeverity.Info,
            $"Subscription quota changed. Honeypots {notification.OldMaxHoneypots} -> {notification.NewMaxHoneypots}; StorageGb {notification.OldMaxStorageGb.ToString("0.####", CultureInfo.InvariantCulture)} -> {notification.NewMaxStorageGb.ToString("0.####", CultureInfo.InvariantCulture)}; NewSourcePlan={SanitizeAuditValue(notification.NewSourcePlanId?.ToString(), MaxAuditFieldLength)}.",
            cancellationToken);
    }

    public Task Handle(QuotaWarningEvent notification, CancellationToken cancellationToken)
    {
        return RecordForSubscriptionAsync(
            notification.SubscriptionId,
            AuditAction.Update,
            AuditSeverity.Warning,
            $"Quota usage warning for {DescribeResourceType(notification.ResourceType)} at {notification.CurrentUsagePercent.ToString("0.##", CultureInfo.InvariantCulture)}% (threshold {notification.WarningThreshold.ToString("0.##", CultureInfo.InvariantCulture)}%).",
            cancellationToken);
    }

    public Task Handle(QuotaExceededEvent notification, CancellationToken cancellationToken)
    {
        var severity = notification.HardLimitEnforced ? AuditSeverity.Error : AuditSeverity.Warning;
        var mode = notification.HardLimitEnforced ? "hard" : "soft";

        return RecordForSubscriptionAsync(
            notification.SubscriptionId,
            AuditAction.Suspend,
            severity,
            $"Quota exceeded ({mode} limit) for {DescribeResourceType(notification.ResourceType)}. Current={notification.CurrentValue.ToString("0.####", CultureInfo.InvariantCulture)}, Max={notification.MaxValue.ToString("0.####", CultureInfo.InvariantCulture)}.",
            cancellationToken);
    }

    public Task Handle(QuotaEnforcementBlockedEvent notification, CancellationToken cancellationToken)
    {
        return RecordForSubscriptionAsync(
            notification.SubscriptionId,
            AuditAction.Suspend,
            AuditSeverity.Error,
            $"Operation blocked by quota enforcement. Operation={SanitizeAuditValue(notification.BlockedOperation, MaxAuditFieldLength)}, Resource={DescribeResourceType(notification.ResourceType)}, Current={notification.CurrentValue.ToString("0.####", CultureInfo.InvariantCulture)}, Max={notification.MaxValue.ToString("0.####", CultureInfo.InvariantCulture)}.",
            cancellationToken);
    }

    public Task Handle(MonthlyUsageFinalizedEvent notification, CancellationToken cancellationToken)
    {
        return RecordForSubscriptionAsync(
            notification.SubscriptionId,
            AuditAction.Archive,
            AuditSeverity.Info,
            $"Monthly usage finalized for {notification.Year:D4}-{notification.Month:D2}. PeakHoneypots={notification.PeakHoneypots}, PeakStorageGb={notification.PeakStorageGb.ToString("0.####", CultureInfo.InvariantCulture)}, ApiCalls={notification.TotalApiCalls}, Overage={notification.OverageCharges.ToString("0.##", CultureInfo.InvariantCulture)}.",
            cancellationToken);
    }

    public Task Handle(MonthlyUsageBilledEvent notification, CancellationToken cancellationToken)
    {
        return RecordForSubscriptionAsync(
            notification.SubscriptionId,
            AuditAction.Update,
            AuditSeverity.Info,
            $"Monthly usage billed. InvoiceId={SanitizeAuditValue(notification.InvoiceId.ToString(), MaxAuditFieldLength)}, Overage={notification.OverageCharges.ToString("0.##", CultureInfo.InvariantCulture)}.",
            cancellationToken);
    }

    private async Task RecordForSubscriptionAsync(
        Guid subscriptionId,
        AuditAction action,
        AuditSeverity severity,
        string reason,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (subscriptionId == Guid.Empty)
        {
            _logger.LogWarning(
                "Skipping subscription quota audit event due to empty subscription id. Action={Action}",
                action);
            return;
        }

        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription is null)
        {
            _logger.LogWarning(
                "Skipping subscription quota audit event because subscription was not found. SubscriptionId={SubscriptionId}, Action={Action}",
                subscriptionId,
                action);
            return;
        }

        if (subscription.OrganizationId == Guid.Empty)
        {
            _logger.LogWarning(
                "Skipping subscription quota audit event due to empty organization id. SubscriptionId={SubscriptionId}, Action={Action}",
                subscriptionId,
                action);
            return;
        }

        try
        {
            await _auditService.RecordAsync(
                subscription.OrganizationId,
                AuditResourceType.Subscription,
                subscriptionId,
                action,
                severity,
                SanitizeAuditValue(reason, MaxAuditReasonLength),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to persist subscription quota audit record. SubscriptionId={SubscriptionId}, Action={Action}",
                subscriptionId,
                action);
        }
    }

    private static string DescribeResourceType(QuotaResourceType resourceType)
    {
        return resourceType switch
        {
            QuotaResourceType.Honeypots => "honeypots",
            QuotaResourceType.Storage => "storage",
            QuotaResourceType.ApiCalls => "api-calls",
            QuotaResourceType.Users => "users",
            _ => "unknown"
        };
    }

    private static string SanitizeAuditValue(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "n/a";
        }

        var trimmed = value.Trim();
        var sb = new StringBuilder(trimmed.Length);
        var previousWasWhitespace = false;

        foreach (var ch in trimmed)
        {
            if (char.IsControl(ch))
            {
                if (!previousWasWhitespace)
                {
                    sb.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                if (!previousWasWhitespace)
                {
                    sb.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            sb.Append(ch);
            previousWasWhitespace = false;

            if (sb.Length >= maxLength)
            {
                break;
            }
        }

        var normalized = sb.ToString().Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "n/a" : normalized;
    }
}
