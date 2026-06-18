using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Organizations.Commands.ResendInvitation;

public sealed record ResendInvitationCommand(
    Guid OrganizationId,
    Guid InvitationId,
    Guid RequestedByUserId,
    int ExpirationDays = 7) : IRequest<Result<string>>;
