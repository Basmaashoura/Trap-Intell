using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Honeypots.Commands.PauseHoneypot;

public sealed record PauseHoneypotCommand(
    Guid OrganizationId,
    Guid HoneypotId,
    string? Reason = null
) : IRequest<Result>;
