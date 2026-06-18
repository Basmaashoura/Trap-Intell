using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Notifications.Commands.MarkAsRead;

public sealed record MarkAsReadCommand(Guid UserId, Guid NotificationId) : IRequest<Result>;
