using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Alerts.Notifications;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications.Enums;

namespace Trap_Intel.Application.Alerts.Commands.ResolveAlert;

internal sealed class ResolveAlertCommandHandler : IRequestHandler<ResolveAlertCommand, Result>
{
    private readonly IAlertRepository _alertRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResolveAlertCommandHandler> _logger;

    public ResolveAlertCommandHandler(
        IAlertRepository alertRepository,
        IUserRepository userRepository,
        INotificationDispatcher notificationDispatcher,
        IUnitOfWork unitOfWork,
        ILogger<ResolveAlertCommandHandler> logger)
    {
        _alertRepository = alertRepository;
        _userRepository = userRepository;
        _notificationDispatcher = notificationDispatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ResolveAlertCommand request, CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(request.AlertId, cancellationToken);

        if (alert is null || alert.OrganizationId != request.OrganizationId)
            return Result.Failure(AlertErrors.NotFound);

        Result result;
        if (request.IsFalsePositive)
        {
            result = alert.MarkAsFalsePositive(request.UserId, request.Resolution);
        }
        else
        {
            result = alert.Resolve(request.UserId, request.Resolution);
        }

        if (result.IsFailure)
            return result;

        await _alertRepository.UpdateAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var notificationType = request.IsFalsePositive
            ? AlertNotificationType.AlertMarkedFalsePositive
            : AlertNotificationType.AlertResolved;

        var message = request.IsFalsePositive
            ? $"Alert '{alert.Title}' was marked as false positive. Reason: {request.Resolution}"
            : $"Alert '{alert.Title}' was resolved. Resolution: {request.Resolution}";

        try
        {
            await AlertNotificationPublisher.PublishAsync(
                alert,
                notificationType,
                message,
                _userRepository,
                _notificationDispatcher,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish alert resolution notification for alert {AlertId}", alert.Id);
        }

        return Result.Success();
    }
}
