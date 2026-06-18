using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Alerts.Commands.ResolveAlert;

public sealed record ResolveAlertCommand(
    Guid OrganizationId,
    Guid AlertId,
    Guid UserId,
    string Resolution,
    bool IsFalsePositive = false
) : IRequest<Result>;
