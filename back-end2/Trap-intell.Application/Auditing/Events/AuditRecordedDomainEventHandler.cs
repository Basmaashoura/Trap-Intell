using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Domain.Notifications;
using Trap_Intel.Domain.Notifications.Enums;
using Trap_Intel.Application.Abstractions.Notifications;
using System.Threading;
using System.Threading.Tasks;

namespace Trap_Intel.Application.Auditing.Events;

internal sealed class AuditRecordedDomainEventHandler : INotificationHandler<AuditRecordedEvent>
{
    private readonly ILogger<AuditRecordedDomainEventHandler> _logger;
    private readonly INotificationDispatcher _notificationDispatcher;

    public AuditRecordedDomainEventHandler(
        ILogger<AuditRecordedDomainEventHandler> logger,
        INotificationDispatcher notificationDispatcher)
    {
        _logger = logger;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task Handle(AuditRecordedEvent notification, CancellationToken cancellationToken)
    {
        var isHighRiskEvent = notification.Severity == AuditSeverity.Critical || notification.Action == AuditAction.Delete;
        if (!isHighRiskEvent)
        {
            return;
        }

        if (!notification.UserId.HasValue || notification.UserId.Value == Guid.Empty)
        {
            _logger.LogWarning(
                "Skipping high-risk audit notification because no user target exists. Organization {OrgId}, Action {Action}, Resource {ResourceType}",
                notification.OrganizationId,
                notification.Action,
                notification.ResourceType);
            return;
        }

        _logger.LogWarning(
            "High-risk audit event detected. Organization {OrgId}, User {UserId}, Action {Action}, Severity {Severity}, Resource {ResourceType}",
            notification.OrganizationId,
            notification.UserId,
            notification.Action,
            notification.Severity,
            notification.ResourceType);

        var priority = notification.Severity == AuditSeverity.Critical
            ? NotificationPriority.Critical
            : NotificationPriority.High;

        var titlePrefix = notification.Severity == AuditSeverity.Critical
            ? "Critical Audit Event"
            : "High-Risk Audit Action";

        var alertEntityResult = Notification.Create(
            userId: notification.UserId.Value,
            type: "AuditAlert",
            title: $"{titlePrefix}: {notification.Action}",
            message: $"Audit event '{notification.Action}' occurred on resource '{notification.ResourceType}' ({notification.ResourceId}).",
            category: NotificationCategory.Security,
            priority: priority
        );

        if (alertEntityResult.IsSuccess)
        {
            await _notificationDispatcher.DispatchAsync(alertEntityResult.Value, cancellationToken);
        }
    }
}
