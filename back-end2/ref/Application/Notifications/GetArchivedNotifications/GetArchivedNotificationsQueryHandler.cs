using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Application.Notifications.Common;
using Domain.Notifications;
using SharedKernel;

namespace Application.Notifications.GetArchivedNotifications;

internal sealed class GetArchivedNotificationsQueryHandler(
    INotificationRepository notificationRepository,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetArchivedNotificationsQuery, PagedResult<NotificationResponse>>
{
    public async Task<Result<PagedResult<NotificationResponse>>> Handle(
        GetArchivedNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        Guid userId = currentUserService.UserId;

        (List<Notification> items, int totalCount) = await notificationRepository.GetArchivedAsync(
            userId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var notifications = items.Select(n => new NotificationResponse(
            n.Id,
            n.Type?.Code ?? string.Empty,
            n.Type?.Name ?? string.Empty,
            n.Title,
            n.Message,
            n.Priority.ToString(),
            n.Type?.IconName,
            n.Type?.ColorHex,
            n.EntityType,
            n.EntityId,
            n.ActionUrl,
            n.ActionText,
            n.IsRead,
            n.ReadAt,
            n.CreatedAt,
            n.CreatedBy?.FullName)).ToList();

        return Result.Success(PagedResult<NotificationResponse>.Create(
            notifications,
            request.Page,
            request.PageSize,
            totalCount));
    }
}
