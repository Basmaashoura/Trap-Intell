using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Alerts.Notifications;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications.Enums;

namespace Trap_Intel.Application.Alerts.Commands.UnsnoozeAlert;

internal sealed class UnsnoozeAlertCommandHandler : IRequestHandler<UnsnoozeAlertCommand, Result>
{
    private readonly IAlertRepository _alertRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnsnoozeAlertCommandHandler> _logger;

    public UnsnoozeAlertCommandHandler(
        IAlertRepository alertRepository,
        IUserRepository userRepository,
        INotificationDispatcher notificationDispatcher,
        IUnitOfWork unitOfWork,
        ILogger<UnsnoozeAlertCommandHandler> logger)
    {
        _alertRepository = alertRepository;
        _userRepository = userRepository;
        _notificationDispatcher = notificationDispatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UnsnoozeAlertCommand request, CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(request.AlertId, cancellationToken);
        if (alert is null)
        {
            return Result.Failure(AlertErrors.NotFound);
        }

        if (alert.OrganizationId != request.OrganizationId)
        {
            return Result.Failure(AlertErrors.NotFound);
        }

        var unsnoozeResult = alert.Unsnooze();
        if (unsnoozeResult.IsFailure)
        {
            return unsnoozeResult;
        }

        await _alertRepository.UpdateAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await AlertNotificationPublisher.PublishAsync(
                alert,
                AlertNotificationType.AlertUnsnoozed,
                $"Alert '{alert.Title}' is active again after snooze expiration/unsnooze.",
                _userRepository,
                _notificationDispatcher,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish alert unsnoozed notification for alert {AlertId}", alert.Id);
        }

        return Result.Success();
    }
}
