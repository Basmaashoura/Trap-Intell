using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Alerts.Notifications;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Alerts.Enums;
using Trap_Intel.Domain.Alerts.ValueObjects;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications.Enums;

namespace Trap_Intel.Application.Auditing.Events;

internal sealed class CriticalAuditLogRecordedDomainEventHandler : INotificationHandler<CriticalAuditLogRecordedEvent>
{
    private readonly IAlertRepository _alertRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CriticalAuditLogRecordedDomainEventHandler> _logger;

    public CriticalAuditLogRecordedDomainEventHandler(
        IAlertRepository alertRepository,
        IUserRepository userRepository,
        INotificationDispatcher notificationDispatcher,
        IUnitOfWork unitOfWork,
        ILogger<CriticalAuditLogRecordedDomainEventHandler> logger)
    {
        _alertRepository = alertRepository;
        _userRepository = userRepository;
        _notificationDispatcher = notificationDispatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(CriticalAuditLogRecordedEvent notification, CancellationToken cancellationToken)
    {
        var alertSource = AlertSource.FromAuditTrail(notification.AuditTrailId, $"SystemEvent_{notification.ResourceType}");

        var alertResult = Alert.Create(
            notification.OrganizationId,
            AlertType.SystemPerformanceIssue, // A mapped generic alert type or Custom
            AlertSeverity.Critical,
            title: $"Critical System Event: {notification.Action}",
            description: $"A critical audit event occurred on {notification.ResourceType} ({notification.ResourceId}).",
            source: alertSource);

        if (alertResult.IsSuccess)
        {
            await _alertRepository.AddAsync(alertResult.Value);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                await AlertNotificationPublisher.PublishAsync(
                    alertResult.Value,
                    AlertNotificationType.AlertCreated,
                    $"Critical audit event '{notification.Action}' created a new alert.",
                    _userRepository,
                    _notificationDispatcher,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish alert-created notification for alert {AlertId}", alertResult.Value.Id);
            }
        }
    }
}
