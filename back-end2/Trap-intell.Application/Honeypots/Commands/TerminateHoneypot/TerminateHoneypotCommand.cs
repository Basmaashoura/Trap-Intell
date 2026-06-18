using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Honeypots.Commands.TerminateHoneypot;

public sealed record TerminateHoneypotCommand(
    Guid OrganizationId,
    Guid HoneypotId,
    string? Reason = null
) : IRequest<Result>;
