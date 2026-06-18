using Application.Abstractions.Messaging;

namespace Application.Notifications.DeletePushToken;

public sealed record DeletePushTokenCommand(Guid TokenId) : ICommand;
