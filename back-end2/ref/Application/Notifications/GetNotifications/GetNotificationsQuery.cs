using Application.Abstractions.Messaging;
using Application.Notifications.Common;
using SharedKernel;

namespace Application.Notifications.GetNotifications;

/// <summary>
/// Query to get paginated notifications for a user.
/// </summary>
public sealed record GetNotificationsQuery(
int Page = 1,
int PageSize = 20,
bool? IsRead = null,
string? Types = null,
string? Priority = null,
DateTime? FromDate = null) : IQuery<PagedResult<NotificationResponse>>;
