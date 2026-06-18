using Application.Abstractions.Messaging;
using Application.Notifications.Common;
using SharedKernel;

namespace Application.Notifications.GetArchivedNotifications;

public sealed record GetArchivedNotificationsQuery(
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<NotificationResponse>>;
