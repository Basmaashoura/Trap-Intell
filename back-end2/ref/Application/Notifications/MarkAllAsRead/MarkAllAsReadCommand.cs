using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Notifications.MarkAllAsRead;

/// <summary>
/// Command to mark all notifications as read for current user.
/// </summary>
public sealed record MarkAllAsReadCommand : ICommand<int>;
