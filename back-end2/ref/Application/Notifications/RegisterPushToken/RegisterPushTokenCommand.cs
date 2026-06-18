using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Notifications.RegisterPushToken;

/// <summary>
/// Command to register a push notification token.
/// </summary>
public sealed record RegisterPushTokenCommand(
string Token,
string Platform,
string? DeviceName) : ICommand<Guid>;
