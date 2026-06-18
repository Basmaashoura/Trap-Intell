using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity.Notifications;

namespace Trap_Intel.Application.Users.Commands.UpdateNotificationSettings;

public sealed record UpdateNotificationSettingsCommand(
    Guid UserId,
    UserNotificationSettings Settings
) : IRequest<Result>;
