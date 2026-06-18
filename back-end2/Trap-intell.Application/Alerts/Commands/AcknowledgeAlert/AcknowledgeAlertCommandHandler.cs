using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Alerts.Notifications;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications.Enums;

namespace Trap_Intel.Application.Alerts.Commands.AcknowledgeAlert;

internal sealed class AcknowledgeAlertCommandHandler : IRequestHandler<AcknowledgeAlertCommand, Result>
{
    private readonly IAlertRepository _alertRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcknowledgeAlertCommandHandler> _logger;

    public AcknowledgeAlertCommandHandler(
        IAlertRepository alertRepository,
        IUserRepository userRepository,
        INotificationDispatcher notificationDispatcher,
        IUnitOfWork unitOfWork,
        ILogger<AcknowledgeAlertCommandHandler> logger)
    {
        _alertRepository = alertRepository;
        _userRepository = userRepository;
        _notificationDispatcher = notificationDispatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AcknowledgeAlertCommand request, CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(request.AlertId, cancellationToken);

        if (alert is null || alert.OrganizationId != request.OrganizationId)
            return Result.Failure(AlertErrors.NotFound);

        var result = alert.Acknowledge(request.UserId);

        if (result.IsFailure)
            return result;

        await _alertRepository.UpdateAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await AlertNotificationPublisher.PublishAsync(
                alert,
                AlertNotificationType.AlertAcknowledged,
                $"Alert '{alert.Title}' was acknowledged. Severity: {alert.Severity}.",
                _userRepository,
                _notificationDispatcher,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish alert acknowledged notification for alert {AlertId}", alert.Id);
        }

        return Result.Success();
    }
}
