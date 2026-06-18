using MediatR;
using Trap_Intel.Application.Abstractions.RealTime;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Notifications;

namespace Trap_Intel.Application.Notifications.Commands.MarkAllAsRead;

internal sealed class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, Result>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAllAsReadCommandHandler(
        INotificationRepository notificationRepository,
        IListRealtimeNotifier listRealtimeNotifier,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _listRealtimeNotifier = listRealtimeNotifier;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
    {
        await _notificationRepository.MarkAllAsReadAsync(request.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _listRealtimeNotifier.NotifyUserListChangedAsync("notifications", request.UserId, action: "bulk-updated", cancellationToken: cancellationToken);
        await _listRealtimeNotifier.NotifyUserListChangedAsync("notifications", request.UserId, filterKey: "unread=true", action: "bulk-updated", cancellationToken: cancellationToken);

        return Result.Success();
    }
}
