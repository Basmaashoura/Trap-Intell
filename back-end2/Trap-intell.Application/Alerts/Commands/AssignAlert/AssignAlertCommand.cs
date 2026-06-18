using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Alerts.Commands.AssignAlert;

public sealed record AssignAlertCommand(
    Guid OrganizationId,
    Guid AlertId,
    Guid TargetUserId,
    Guid AssignedByUserId
) : IRequest<Result>;
