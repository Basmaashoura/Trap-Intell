using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Alerts.Notifications;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications.Enums;

namespace Trap_Intel.Application.Alerts.Commands.AssignAlert;

internal sealed class AssignAlertCommandHandler : IRequestHandler<AssignAlertCommand, Result>
{
    private readonly IAlertRepository _alertRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignAlertCommandHandler> _logger;

    public AssignAlertCommandHandler(
        IAlertRepository alertRepository,
        IUserRepository userRepository,
        INotificationDispatcher notificationDispatcher,
        IUnitOfWork unitOfWork,
        ILogger<AssignAlertCommandHandler> logger)
    {
        _alertRepository = alertRepository;
        _userRepository = userRepository;
        _notificationDispatcher = notificationDispatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignAlertCommand request, CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(request.AlertId, cancellationToken);

        if (alert is null || alert.OrganizationId != request.OrganizationId)
            return Result.Failure(AlertErrors.NotFound);

        // Assumption: Target user belongs to same Organization checking is handled upstream 
        // or through Identity Domain but functionally this triggers the Action

        var result = alert.AssignTo(request.TargetUserId, request.AssignedByUserId);

        if (result.IsFailure)
            return result;

        await _alertRepository.UpdateAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await AlertNotificationPublisher.PublishAsync(
                alert,
                AlertNotificationType.AlertAssigned,
                $"Alert '{alert.Title}' has been assigned to a responder.",
                _userRepository,
                _notificationDispatcher,
                cancellationToken,
                request.TargetUserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish alert assigned notification for alert {AlertId}", alert.Id);
        }

        return Result.Success();
    }
}
