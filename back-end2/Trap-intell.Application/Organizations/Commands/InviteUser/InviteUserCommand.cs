using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;

namespace Trap_Intel.Application.Organizations.Commands.InviteUser;

public sealed record InviteUserCommand(
    Guid OrganizationId,
    string Email,
    Guid RoleId,
    Guid InvitedByUserId, // Ideally this comes from ICurrentUserService in a pipeline, but explicit for now
    string? PersonalMessage = null,
    int ExpirationDays = 7
) : IRequest<Result<string>>; // Returns the raw token that should be emailed

