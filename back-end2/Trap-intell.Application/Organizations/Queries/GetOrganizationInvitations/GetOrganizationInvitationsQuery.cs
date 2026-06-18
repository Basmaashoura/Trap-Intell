using MediatR;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Invitations.Enums;

namespace Trap_Intel.Application.Organizations.Queries.GetOrganizationInvitations;

public sealed record OrganizationInvitationDto(
    Guid Id,
    Guid OrganizationId,
    string Email,
    Guid RoleId,
    Guid InvitedByUserId,
    string Status,
    string? PersonalMessage,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime UpdatedAt,
    DateTime? AcceptedAt,
    DateTime? DeclinedAt,
    DateTime? RevokedAt,
    bool IsExpired);

public sealed record GetOrganizationInvitationsQuery(
    Guid OrganizationId,
    InvitationStatus? Status = null,
    GlobalQueryOptions? Query = null) : IRequest<Result<PagedResult<OrganizationInvitationDto>>>;
