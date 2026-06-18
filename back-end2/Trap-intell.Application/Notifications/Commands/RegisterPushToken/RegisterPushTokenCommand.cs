using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Notifications.Enums;

namespace Trap_Intel.Application.Notifications.Commands.RegisterPushToken;

public sealed record RegisterPushTokenCommand(
    Guid UserId,
    string Token,
    PushPlatform Platform,
    string DeviceId
) : IRequest<Result>;
