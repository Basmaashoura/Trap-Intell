using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Notifications.MarkAsRead;

/// <summary>
/// Command to mark a notification as read.
/// </summary>
public sealed record MarkAsReadCommand(Guid NotificationId) : ICommand;
