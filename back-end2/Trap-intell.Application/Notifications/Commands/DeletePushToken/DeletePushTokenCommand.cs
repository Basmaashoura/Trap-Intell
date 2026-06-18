using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Notifications.Commands.DeletePushToken;

public sealed record DeletePushTokenCommand(Guid UserId, string Token) : IRequest<Result>;
