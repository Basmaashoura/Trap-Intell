using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Alerts.Notifications;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Alerts.Enums;
using Trap_Intel.Domain.Alerts.ValueObjects;
using Trap_Intel.Domain.Attacks.Enums;
using Trap_Intel.Domain.Attacks.Events;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications.Enums;

namespace Trap_Intel.Application.Attacks.Events;

internal sealed class AttackDomainEventAlertHandler :
    INotificationHandler<HighSeverityAttackDetectedEvent>,
    INotificationHandler<MalwareUploadedEvent>
{
    private readonly IAlertRepository _alertRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AttackDomainEventAlertHandler> _logger;

    public AttackDomainEventAlertHandler(
        IAlertRepository alertRepository,
        IUserRepository userRepository,
        INotificationDispatcher notificationDispatcher,
        IUnitOfWork unitOfWork,
        ILogger<AttackDomainEventAlertHandler> logger)
    {
        _alertRepository = alertRepository;
        _userRepository = userRepository;
        _notificationDispatcher = notificationDispatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(HighSeverityAttackDetectedEvent notification, CancellationToken cancellationToken)
    {
        if (await HasAlertForAttackAsync(notification.OrganizationId, notification.AttackEventId, AlertType.HighSeverityAttack, cancellationToken))
        {
            return;
        }

        var source = AlertSource.FromAttackEvent(notification.AttackEventId, notification.SourceIP);

        var alertResult = Alert.Create(
            notification.OrganizationId,
            AlertType.HighSeverityAttack,
            MapSeverity(notification.Severity),
            title: $"High-severity attack detected from {notification.SourceIP}",
            description: $"Attack event {notification.AttackEventId} targeted honeypot {notification.HoneypotId}. Severity={notification.Severity}, ThreatScore={notification.ThreatScore:0.##}.",
            source: source,
            expiresIn: TimeSpan.FromDays(7));

        if (alertResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create high-severity attack alert. AttackEventId={AttackEventId}, Errors={Errors}",
                notification.AttackEventId,
                string.Join(',', alertResult.Errors.Select(error => error.Code)));
            return;
        }

        await PersistAndNotifyAsync(
            alertResult.Value,
            $"High-severity attack detected from {notification.SourceIP}.",
            cancellationToken);
    }

    public async Task Handle(MalwareUploadedEvent notification, CancellationToken cancellationToken)
    {
        if (await HasAlertForAttackAsync(notification.OrganizationId, notification.AttackEventId, AlertType.MalwareDetected, cancellationToken))
        {
            return;
        }

        var source = AlertSource.FromAttackEvent(notification.AttackEventId, notification.SourceIP);

        var alertResult = Alert.Create(
            notification.OrganizationId,
            AlertType.MalwareDetected,
            AlertSeverity.Critical,
            title: "Malware upload detected",
            description: $"Malware payload uploaded on honeypot {notification.HoneypotId}. FileHash={notification.FileHash}, FileSize={notification.FileSize} bytes, SourceIP={notification.SourceIP}.",
            source: source,
            expiresIn: TimeSpan.FromDays(14));

        if (alertResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create malware upload alert. AttackEventId={AttackEventId}, Errors={Errors}",
                notification.AttackEventId,
                string.Join(',', alertResult.Errors.Select(error => error.Code)));
            return;
        }

        await PersistAndNotifyAsync(
            alertResult.Value,
            $"Malware upload detected from {notification.SourceIP}.",
            cancellationToken);
    }

    private async Task<bool> HasAlertForAttackAsync(
        Guid organizationId,
        Guid attackEventId,
        AlertType alertType,
        CancellationToken cancellationToken)
    {
        var alerts = await _alertRepository.GetBySourceIdAsync(attackEventId, cancellationToken);

        return alerts.Any(alert =>
            alert.OrganizationId == organizationId &&
            alert.AlertType == alertType);
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
            _logger.LogWarning(ex, "Failed to publish attack alert notification for alert {AlertId}", alert.Id);
        }
    }

    private static AlertSeverity MapSeverity(AttackSeverity severity)
    {
        return severity switch
        {
            AttackSeverity.Critical => AlertSeverity.Critical,
            AttackSeverity.High => AlertSeverity.High,
            AttackSeverity.Medium => AlertSeverity.Medium,
            _ => AlertSeverity.Low
        };
    }
}
