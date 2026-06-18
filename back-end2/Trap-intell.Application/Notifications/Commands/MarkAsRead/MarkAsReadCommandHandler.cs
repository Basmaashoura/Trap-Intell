using MediatR;
using Trap_Intel.Application.Abstractions.RealTime;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Notifications;

namespace Trap_Intel.Application.Notifications.Commands.MarkAsRead;

internal sealed class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Result>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAsReadCommandHandler(
        INotificationRepository notificationRepository,
        IListRealtimeNotifier listRealtimeNotifier,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _listRealtimeNotifier = listRealtimeNotifier;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification == null || notification.UserId != request.UserId)
        {
            return Result.Failure(NotificationErrors.NotFound);
        }

        var markResult = notification.MarkAsRead();
        if (markResult.IsFailure)
        {
            return markResult;
        }

        await _notificationRepository.UpdateAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var payload = new { notificationId = request.NotificationId };
        await _listRealtimeNotifier.NotifyUserListChangedAsync("notifications", request.UserId, action: "updated", payload: payload, cancellationToken: cancellationToken);
        await _listRealtimeNotifier.NotifyUserListChangedAsync("notifications", request.UserId, filterKey: "unread=true", action: "updated", payload: payload, cancellationToken: cancellationToken);

        return Result.Success();
    }
}
