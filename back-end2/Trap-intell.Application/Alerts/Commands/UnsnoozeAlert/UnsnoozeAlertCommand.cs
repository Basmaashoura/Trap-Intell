using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Alerts.Commands.UnsnoozeAlert;

public sealed record UnsnoozeAlertCommand(
    Guid OrganizationId,
    Guid AlertId
) : IRequest<Result>;
