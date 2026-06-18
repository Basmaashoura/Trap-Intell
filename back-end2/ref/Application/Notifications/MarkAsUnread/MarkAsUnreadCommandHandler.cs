using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Abstractions.Notifications;
using Application.Abstractions.Repositories;
using Domain.Notifications;
using SharedKernel;

namespace Application.Notifications.MarkAsUnread;

internal sealed class MarkAsUnreadCommandHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IRealtimeNotificationService realtimeService)
    : ICommandHandler<MarkAsUnreadCommand>
{
    public async Task<Result> Handle(
        MarkAsUnreadCommand request,
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

        if (!notification.IsRead)
        {
            return Result.Success(); // Already unread
        }

        notification.MarkAsUnread();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Update unread count
        int unreadCount = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken);
        await realtimeService.NotifyUnreadCountAsync(userId, unreadCount, cancellationToken);

        return Result.Success();
    }
}
