using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Honeypots;

namespace Trap_Intel.Application.Honeypots.Commands.ResumeHoneypot;

public sealed record ResumeHoneypotCommand(
    Guid OrganizationId,
    Guid HoneypotId,
    string? Reason = null
) : IRequest<Result>;
