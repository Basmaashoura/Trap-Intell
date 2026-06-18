using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Notifications.DismissNotification;

/// <summary>
/// Command to dismiss a notification.
/// </summary>
public sealed record DismissNotificationCommand(Guid NotificationId) : ICommand;
