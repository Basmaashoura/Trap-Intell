using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Alerts.Notifications;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Alerts.Enums;
using Trap_Intel.Domain.Alerts.ValueObjects;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications.Enums;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Domain.Subscriptions.Events;

namespace Trap_Intel.Application.Subscriptions.Events;

internal sealed class SubscriptionQuotaAlertDomainEventHandler :
    INotificationHandler<QuotaWarningEvent>,
    INotificationHandler<QuotaExceededEvent>,
    INotificationHandler<QuotaEnforcementBlockedEvent>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubscriptionQuotaAlertDomainEventHandler> _logger;

    public SubscriptionQuotaAlertDomainEventHandler(
        ISubscriptionRepository subscriptionRepository,
        IAlertRepository alertRepository,
        IUserRepository userRepository,
        INotificationDispatcher notificationDispatcher,
        IUnitOfWork unitOfWork,
        ILogger<SubscriptionQuotaAlertDomainEventHandler> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _alertRepository = alertRepository;
        _userRepository = userRepository;
        _notificationDispatcher = notificationDispatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(QuotaWarningEvent notification, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(notification.SubscriptionId, cancellationToken);
        if (subscription is null)
        {
            _logger.LogWarning(
                "Skipping quota warning alert because subscription was not found. SubscriptionId={SubscriptionId}",
                notification.SubscriptionId);
            return;
        }

        var alertType = AlertType.QuotaExceeded;
        var resource = DescribeResource(notification.ResourceType);
        var sourceName = $"QuotaWarning_{resource}";

        if (await HasRecentOpenAlertAsync(
                subscription.OrganizationId,
                subscription.Id,
                alertType,
                sourceName,
                cancellationToken))
        {
            return;
        }

        var source = new AlertSource
        {
            SourceType = "Subscription",
            SourceId = subscription.Id,
            SourceName = sourceName
        };

        var alertResult = Alert.Create(
            subscription.OrganizationId,
            alertType,
            AlertSeverity.Medium,
            title: $"Quota warning: {resource}",
            description: $"Subscription {subscription.Id} reached {notification.CurrentUsagePercent:0.##}% of {resource} quota (warning threshold is {notification.WarningThreshold:0.##}%).",
            source: source,
            expiresIn: TimeSpan.FromDays(2));

        if (alertResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create quota warning alert for subscription {SubscriptionId}. Errors={Errors}",
                subscription.Id,
                string.Join(",", alertResult.Errors.Select(error => error.Code)));
            return;
        }

        await PersistAndNotifyAsync(
            alertResult.Value,
            $"Quota warning for {resource} on subscription {subscription.Id}. Current usage is {notification.CurrentUsagePercent:0.##}%.",
            cancellationToken);
    }

    public async Task Handle(QuotaExceededEvent notification, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(notification.SubscriptionId, cancellationToken);
        if (subscription is null)
        {
            _logger.LogWarning(
                "Skipping quota exceeded alert because subscription was not found. SubscriptionId={SubscriptionId}",
                notification.SubscriptionId);
            return;
        }

        var severity = notification.HardLimitEnforced ? AlertSeverity.Critical : AlertSeverity.High;
        var alertType = AlertType.QuotaExceeded;

        var resource = DescribeResource(notification.ResourceType);
        var sourceName = $"Quota_{resource}";

        var source = new AlertSource
        {
            SourceType = "Subscription",
            SourceId = subscription.Id,
            SourceName = sourceName
        };

        if (await HasRecentOpenAlertAsync(
                subscription.OrganizationId,
                subscription.Id,
                alertType,
                sourceName,
                cancellationToken))
        {
            return;
        }

        var alertResult = Alert.Create(
            subscription.OrganizationId,
            alertType,
            severity,
            title: $"Quota exceeded: {resource}",
            description: $"Subscription {subscription.Id} exceeded {resource} quota. Current value is {notification.CurrentValue:0.####} while the maximum allowed is {notification.MaxValue:0.####}.",
            source: source,
            expiresIn: TimeSpan.FromDays(3));

        if (alertResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create quota exceeded alert for subscription {SubscriptionId}. Errors={Errors}",
                subscription.Id,
                string.Join(",", alertResult.Errors.Select(error => error.Code)));
            return;
        }

        await PersistAndNotifyAsync(
            alertResult.Value,
            $"Quota exceeded for {resource} on subscription {subscription.Id}.",
            cancellationToken);
    }

    public async Task Handle(QuotaEnforcementBlockedEvent notification, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(notification.SubscriptionId, cancellationToken);
        if (subscription is null)
        {
            _logger.LogWarning(
                "Skipping quota enforcement alert because subscription was not found. SubscriptionId={SubscriptionId}",
                notification.SubscriptionId);
            return;
        }

        var alertType = AlertType.QuotaExceeded;
        var resource = DescribeResource(notification.ResourceType);
        var sourceName = $"QuotaBlock_{resource}";

        if (await HasRecentOpenAlertAsync(
                subscription.OrganizationId,
                subscription.Id,
                alertType,
                sourceName,
                cancellationToken))
        {
            return;
        }

        var source = new AlertSource
        {
            SourceType = "Subscription",
            SourceId = subscription.Id,
            SourceName = sourceName
        };

        var alertResult = Alert.Create(
            subscription.OrganizationId,
            alertType,
            AlertSeverity.Critical,
            title: $"Quota enforcement blocked operation: {notification.BlockedOperation}",
            description: $"Operation '{notification.BlockedOperation}' was blocked for subscription {subscription.Id} due to {resource} quota. Current value is {notification.CurrentValue:0.####} and max allowed is {notification.MaxValue:0.####}.",
            source: source,
            expiresIn: TimeSpan.FromDays(7));

        if (alertResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create quota enforcement alert for subscription {SubscriptionId}. Errors={Errors}",
                subscription.Id,
                string.Join(",", alertResult.Errors.Select(error => error.Code)));
            return;
        }

        await PersistAndNotifyAsync(
            alertResult.Value,
            $"Operation '{notification.BlockedOperation}' was blocked by quota enforcement for subscription {subscription.Id}.",
            cancellationToken);
    }

    private async Task PersistAndNotifyAsync(Alert alert, string notificationMessage, CancellationToken cancellationToken)
    {
        await _alertRepository.AddAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await AlertNotificationPublisher.PublishAsync(
                alert,
                AlertNotificationType.AlertCreated,
                notificationMessage,
                _userRepository,
                _notificationDispatcher,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish quota alert notification for alert {AlertId}", alert.Id);
        }
    }

    private async Task<bool> HasRecentOpenAlertAsync(
        Guid organizationId,
        Guid subscriptionId,
        AlertType alertType,
        string sourceName,
        CancellationToken cancellationToken)
    {
        var recentAlerts = await _alertRepository.GetRecentAsync(organizationId, hours: 1, cancellationToken);

        return recentAlerts.Any(alert =>
            alert.AlertType == alertType &&
            alert.Source.SourceId == subscriptionId &&
            string.Equals(alert.Source.SourceName, sourceName, StringComparison.OrdinalIgnoreCase) &&
            IsOpenStatus(alert.Status));
    }

    private static bool IsOpenStatus(AlertStatus status)
    {
        return status == AlertStatus.New ||
               status == AlertStatus.Acknowledged ||
               status == AlertStatus.InProgress ||
               status == AlertStatus.Escalated ||
               status == AlertStatus.Snoozed;
    }

    private static string DescribeResource(QuotaResourceType resourceType)
    {
        return resourceType switch
        {
            QuotaResourceType.Honeypots => "honeypots",
            QuotaResourceType.Storage => "storage",
            QuotaResourceType.ApiCalls => "api-calls",
            QuotaResourceType.Users => "users",
            _ => "unknown-resource"
        };
    }
}
