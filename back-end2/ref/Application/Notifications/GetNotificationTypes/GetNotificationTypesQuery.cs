using Application.Abstractions.Messaging;
using Application.Notifications.Common;
using SharedKernel;

namespace Application.Notifications.GetNotificationTypes;

/// <summary>
/// Query to get all notification types.
/// </summary>
public sealed record GetNotificationTypesQuery(
string? Category = null) : IQuery<List<NotificationTypeResponse>>;
