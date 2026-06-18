using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Abstractions.Notifications;
using Application.Abstractions.Repositories;
using Domain.Notifications;
using SharedKernel;

namespace Application.Notifications.MarkAllAsRead;

internal sealed class MarkAllAsReadCommandHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IRealtimeNotificationService realtimeService)
    : ICommandHandler<MarkAllAsReadCommand, int>
{
    public async Task<Result<int>> Handle(
        MarkAllAsReadCommand request,
        CancellationToken cancellationToken)
    {
        Guid userId = currentUserService.UserId;

        List<Notification> unreadNotifications = await notificationRepository.GetUnreadAsync(
            userId,
            cancellationToken);

        foreach (Notification notification in unreadNotifications)
        {
            notification.MarkAsRead();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify client via SignalR
        await realtimeService.NotifyAllReadAsync(userId, cancellationToken);
        await realtimeService.NotifyUnreadCountAsync(userId, 0, cancellationToken);

        return Result.Success(unreadNotifications.Count);
    }
}
