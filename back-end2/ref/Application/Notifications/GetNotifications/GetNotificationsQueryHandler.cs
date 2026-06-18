using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Application.Notifications.Common;
using Domain.Notifications;
using SharedKernel;

namespace Application.Notifications.GetNotifications;

internal sealed class GetNotificationsQueryHandler(
    INotificationRepository notificationRepository,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetNotificationsQuery, PagedResult<NotificationResponse>>
{
    public async Task<Result<PagedResult<NotificationResponse>>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        Guid userId = currentUserService.UserId;

        // Parse types
        IEnumerable<string>? typeCodes = null;
        if (!string.IsNullOrEmpty(request.Types))
        {
            typeCodes = request.Types.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        // Parse priorities
        IEnumerable<NotificationPriority>? priorities = null;
        if (!string.IsNullOrEmpty(request.Priority))
        {
            string[] priorityStrings = request.Priority.Split(',', StringSplitOptions.RemoveEmptyEntries);
            priorities = priorityStrings
                .Select(p => Enum.TryParse<NotificationPriority>(p, true, out NotificationPriority result) ? result : (NotificationPriority?)null)
                .Where(p => p.HasValue)
                .Select(p => p!.Value);
        }

        (List<Notification> notifications, int totalCount) = await notificationRepository.GetPaginatedAsync(
            userId,
            request.Page,
            request.PageSize,
            request.IsRead,
            typeCodes,
            priorities,
            request.FromDate,
            cancellationToken);

        // Map to response
        var items = notifications.Select(n => new NotificationResponse(
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

        var result = new PagedResult<NotificationResponse>
        {
            Items = items,
            PageNumber = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result.Success(result);
    }
}
