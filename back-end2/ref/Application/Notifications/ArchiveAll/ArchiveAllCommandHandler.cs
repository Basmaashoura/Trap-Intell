using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Domain.Notifications;
using SharedKernel;

namespace Application.Notifications.ArchiveAll;

internal sealed class ArchiveAllCommandHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : ICommandHandler<ArchiveAllCommand, int>
{
    public async Task<Result<int>> Handle(
        ArchiveAllCommand request,
        CancellationToken cancellationToken)
    {
        Guid userId = currentUserService.UserId;

        List<Notification> readNotifications = await notificationRepository.GetReadNotArchivedAsync(
            userId,
            cancellationToken);

        foreach (Notification notification in readNotifications)
        {
            notification.Archive();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(readNotifications.Count);
    }
}
