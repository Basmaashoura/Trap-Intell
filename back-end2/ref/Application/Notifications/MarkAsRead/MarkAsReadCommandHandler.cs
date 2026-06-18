using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Abstractions.Notifications;
using Application.Abstractions.Repositories;
using Domain.Notifications;
using SharedKernel;

namespace Application.Notifications.MarkAsRead;

internal sealed class MarkAsReadCommandHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IRealtimeNotificationService realtimeService)
    : ICommandHandler<MarkAsReadCommand>
{
    public async Task<Result> Handle(
        MarkAsReadCommand request,
        CancellationToken cancellationToken)
    {
        Guid userId = currentUserService.UserId;

        Notification? notification = await notificationRepository.GetByIdAsync(
            request.NotificationId,
            userId,
            cancellationToken);

        if (notification is null)
        {
            return Result.Failure(NotificationErrors.NotFound(request.NotificationId));
        }

        notification.MarkAsRead();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify client via SignalR
        await realtimeService.NotifyReadAsync(userId, request.NotificationId, cancellationToken);

        // Update unread count
        int unreadCount = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken);

        await realtimeService.NotifyUnreadCountAsync(userId, unreadCount, cancellationToken);

        return Result.Success();
    }
}
