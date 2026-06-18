using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Alerts.Commands.SnoozeAlert;

public sealed record SnoozeAlertCommand(
    Guid OrganizationId,
    Guid AlertId,
    Guid UserId,
    TimeSpan Duration,
    string? Reason = null
) : IRequest<Result>;
