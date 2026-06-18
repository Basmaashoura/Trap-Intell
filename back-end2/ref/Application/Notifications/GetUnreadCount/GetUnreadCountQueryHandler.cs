using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using SharedKernel;

namespace Application.Notifications.GetUnreadCount;

internal sealed class GetUnreadCountQueryHandler(
    INotificationRepository notificationRepository,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetUnreadCountQuery, int>
{
    public async Task<Result<int>> Handle(
        GetUnreadCountQuery request,
        CancellationToken cancellationToken)
    {
        Guid userId = currentUserService.UserId;

        int count = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken);

        return Result.Success(count);
    }
}
