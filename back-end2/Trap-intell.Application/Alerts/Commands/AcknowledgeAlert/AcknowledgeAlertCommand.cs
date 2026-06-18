using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Alerts.Commands.AcknowledgeAlert;

public sealed record AcknowledgeAlertCommand(
    Guid OrganizationId,
    Guid AlertId,
    Guid UserId
) : IRequest<Result>;
