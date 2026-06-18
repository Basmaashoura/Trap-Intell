using MediatR;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Notifications;

namespace Trap_Intel.Application.Notifications.Queries.GetNotifications;

internal sealed class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, Result<PagedResult<NotificationDto>>>
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Result<PagedResult<NotificationDto>>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var queryOptions = request.Query ?? new GlobalQueryOptions();
        var pageNumber = queryOptions.GetPageNumber();
        var pageSize = queryOptions.GetPageSize();
        var searchTerm = queryOptions.GetSearchTerm();

        var (notifications, totalCount) = await _notificationRepository.SearchUserNotificationsAsync(
            request.UserId,
            request.UnreadOnly,
            request.Category,
            request.Priority,
            searchTerm,
            queryOptions.SortBy,
            queryOptions.IsSortDescending(),
            pageNumber,
            pageSize,
            cancellationToken);

        var dtos = notifications.Select(n => new NotificationDto(
            n.Id,
            n.UserId,
            n.Type,
            n.Category,
            n.Priority,
            n.Title,
            n.Message,
            n.LinkUri,
            n.RelatedEntityId,
            n.CreatedAt,
            n.ReadAt,
            n.ExpiresAt,
            n.IsRead,
            n.IsDismissed
        )).ToList();

        var result = new PagedResult<NotificationDto>(dtos, pageNumber, pageSize, totalCount);
        return Result.Success(result);
    }
}
