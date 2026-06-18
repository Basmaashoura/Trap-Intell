using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Organizations.Commands.RevokeInvitation;

public sealed record RevokeInvitationCommand(
    Guid OrganizationId,
    Guid InvitationId,
    Guid RevokedByUserId,
    string Reason) : IRequest<Result>;
