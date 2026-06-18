using Application.Abstractions.Messaging;

namespace Application.Notifications.MarkAsUnread;

public sealed record MarkAsUnreadCommand(Guid NotificationId) : ICommand;
