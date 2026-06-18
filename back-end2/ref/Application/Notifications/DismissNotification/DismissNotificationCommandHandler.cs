using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Abstractions.Notifications;
using Application.Abstractions.Repositories;
using Domain.Notifications;
using SharedKernel;

namespace Application.Notifications.DismissNotification;

internal sealed class DismissNotificationCommandHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IRealtimeNotificationService realtimeService)
    : ICommandHandler<DismissNotificationCommand>
{
    public async Task<Result> Handle(
        DismissNotificationCommand request,
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

        bool wasUnread = !notification.IsRead;
        notification.Dismiss();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Update unread count if the dismissed notification was unread
        if (wasUnread)
        {
            int unreadCount = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken);
            await realtimeService.NotifyUnreadCountAsync(userId, unreadCount, cancellationToken);
        }

        return Result.Success();
    }
}
