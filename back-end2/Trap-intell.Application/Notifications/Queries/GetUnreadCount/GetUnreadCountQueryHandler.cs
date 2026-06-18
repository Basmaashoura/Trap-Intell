using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Notifications;

namespace Trap_Intel.Application.Notifications.Queries.GetUnreadCount;

internal sealed class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, Result<int>>
{
    private readonly INotificationRepository _notificationRepository;

    public GetUnreadCountQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var count = await _notificationRepository.GetUnreadCountAsync(request.UserId, cancellationToken);
        return Result.Success(count);
    }
}
