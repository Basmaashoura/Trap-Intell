using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Notifications.GetUnreadCount;

/// <summary>
/// Query to get unread notification count for current user.
/// </summary>
public sealed record GetUnreadCountQuery : IQuery<int>;
