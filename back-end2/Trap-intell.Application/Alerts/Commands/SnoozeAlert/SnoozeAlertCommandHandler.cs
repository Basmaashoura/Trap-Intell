using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Alerts.Notifications;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications.Enums;

namespace Trap_Intel.Application.Alerts.Commands.SnoozeAlert;

internal sealed class SnoozeAlertCommandHandler : IRequestHandler<SnoozeAlertCommand, Result>
{
    private readonly IAlertRepository _alertRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SnoozeAlertCommandHandler> _logger;

    public SnoozeAlertCommandHandler(
        IAlertRepository alertRepository,
        IUserRepository userRepository,
        INotificationDispatcher notificationDispatcher,
        IUnitOfWork unitOfWork,
        ILogger<SnoozeAlertCommandHandler> logger)
    {
        _alertRepository = alertRepository;
        _userRepository = userRepository;
        _notificationDispatcher = notificationDispatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(SnoozeAlertCommand request, CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(request.AlertId, cancellationToken);
        if (alert is null)
        {
            return Result.Failure(AlertErrors.NotFound);
        }

        if (alert.OrganizationId != request.OrganizationId)
        {
            return Result.Failure(AlertErrors.NotFound); // Don't leak organization data
        }

        var snoozeResult = alert.Snooze(request.UserId, request.Duration, request.Reason);
        if (snoozeResult.IsFailure)
        {
            return snoozeResult;
        }

        await _alertRepository.UpdateAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var reasonPart = string.IsNullOrWhiteSpace(request.Reason)
                ? ""
                : $" Reason: {request.Reason}";

            await AlertNotificationPublisher.PublishAsync(
                alert,
                AlertNotificationType.AlertSnoozed,
                $"Alert '{alert.Title}' was snoozed for {request.Duration.TotalMinutes:0} minutes.{reasonPart}",
                _userRepository,
                _notificationDispatcher,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish alert snoozed notification for alert {AlertId}", alert.Id);
        }

        return Result.Success();
    }
}
