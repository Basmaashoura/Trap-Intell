using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Notifications.Commands.MarkAllAsRead;

public sealed record MarkAllAsReadCommand(Guid UserId) : IRequest<Result>;
