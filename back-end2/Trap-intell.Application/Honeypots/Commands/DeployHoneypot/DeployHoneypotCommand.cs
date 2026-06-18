using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Honeypots;

namespace Trap_Intel.Application.Honeypots.Commands.DeployHoneypot;

public sealed record DeployHoneypotCommand(
    Guid OrganizationId,
    Guid SubscriptionId,
    string Name,
    HoneypotType Type,
    HoneypotDeploymentLocation Location,
    string ConfigTemplateBase64
) : IRequest<Result>;
