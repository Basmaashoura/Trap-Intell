using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Alerts.Enums;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Application.Auditing.Events;

internal sealed class AuditLogAcknowledgedDomainEventHandler : INotificationHandler<AuditLogAcknowledgedEvent>
{
    private readonly IAlertRepository _alertRepository;
    private readonly ILogger<AuditLogAcknowledgedDomainEventHandler> _logger;

    public AuditLogAcknowledgedDomainEventHandler(
        IAlertRepository alertRepository,
        ILogger<AuditLogAcknowledgedDomainEventHandler> logger)
    {
        _alertRepository = alertRepository;
        _logger = logger;
    }

    public async Task Handle(AuditLogAcknowledgedEvent notification, CancellationToken cancellationToken)
    {
        // 1. Fetch any linked alerts generated from this audit log.
        // CriticalAuditLogRecordedEvent linked the new Alert using AlertSource.FromAuditTrail(AuditTrailId)
        var linkedAlerts = await _alertRepository.GetBySourceIdAsync(notification.AuditTrailId, cancellationToken);

        foreach (var alert in linkedAlerts)
        {
            if (alert.Status == AlertStatus.Resolved || alert.Status == AlertStatus.FalsePositive)
            {
                continue; // Already closed
            }

            // 2. Acknowledge and Resolve the Alert to keep states in sync natively!
            if (alert.Status == AlertStatus.New)
            {
                var ackResult = alert.Acknowledge(notification.AcknowledgedBy);
                if (ackResult.IsFailure)
                {
                    _logger.LogWarning("Failed to auto-acknowledge linked alert {AlertId}: {Error}", alert.Id, ackResult.Errors[0].Message);
                }
            }

            var resolveResult = alert.Resolve(
                notification.AcknowledgedBy, 
                resolution: $"Auto-resolved. The associated critical audit log was acknowledged by the administrator at {notification.AcknowledgedAt:O}."
            );

            if (resolveResult.IsSuccess)
            {
                await _alertRepository.UpdateAsync(alert, cancellationToken);
                _logger.LogInformation("Alert {AlertId} successfully auto-resolved via Audit Log synchronization.", alert.Id);
            }
            else
            {
                _logger.LogWarning("Failed to auto-resolve linked alert {AlertId}: {Error}", alert.Id, resolveResult.Errors[0].Message);
            }
        }
    }
}
