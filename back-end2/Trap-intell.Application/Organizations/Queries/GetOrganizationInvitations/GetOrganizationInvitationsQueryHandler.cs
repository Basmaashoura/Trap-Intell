using MediatR;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Invitations;
using Trap_Intel.Domain.Invitations.Enums;

namespace Trap_Intel.Application.Organizations.Queries.GetOrganizationInvitations;

internal sealed class GetOrganizationInvitationsQueryHandler : IRequestHandler<GetOrganizationInvitationsQuery, Result<PagedResult<OrganizationInvitationDto>>>
{
    private readonly IOrganizationInvitationRepository _invitationRepository;

    public GetOrganizationInvitationsQueryHandler(IOrganizationInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
    }

    public async Task<Result<PagedResult<OrganizationInvitationDto>>> Handle(
        GetOrganizationInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        var queryOptions = request.Query ?? new GlobalQueryOptions();
        var pageNumber = queryOptions.GetPageNumber();
        var pageSize = queryOptions.GetPageSize();
        var searchTerm = queryOptions.GetSearchTerm();

        var invitations = request.Status.HasValue
            ? await _invitationRepository.GetByStatusAsync(request.OrganizationId, request.Status.Value, cancellationToken)
            : await _invitationRepository.GetByOrganizationAsync(request.OrganizationId, cancellationToken);

        IEnumerable<OrganizationInvitation> filteredInvitations = invitations;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredInvitations = filteredInvitations.Where(i =>
                i.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(i.PersonalMessage) && i.PersonalMessage.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        var sortedInvitations = ApplySort(filteredInvitations, queryOptions.SortBy, queryOptions.IsSortDescending());
        var totalCount = sortedInvitations.Count();

        var pagedInvitations = sortedInvitations
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var invitationDtos = pagedInvitations.Select(invitation => new OrganizationInvitationDto(
            invitation.Id,
            invitation.OrganizationId,
            invitation.Email,
            invitation.RoleId,
            invitation.InvitedByUserId,
            invitation.Status.ToString(),
            invitation.PersonalMessage,
            invitation.CreatedAt,
            invitation.ExpiresAt,
            invitation.UpdatedAt,
            invitation.AcceptedAt,
            invitation.DeclinedAt,
            invitation.RevokedAt,
            invitation.IsExpired)).ToList();

        var result = new PagedResult<OrganizationInvitationDto>(invitationDtos, pageNumber, pageSize, totalCount);
        return Result.Success(result);
    }

    private static IEnumerable<OrganizationInvitation> ApplySort(
        IEnumerable<OrganizationInvitation> invitations,
        string? sortBy,
        bool descending)
    {
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

        return normalizedSortBy switch
        {
            "email" => descending
                ? invitations.OrderByDescending(i => i.Email, StringComparer.OrdinalIgnoreCase)
                : invitations.OrderBy(i => i.Email, StringComparer.OrdinalIgnoreCase),

            "status" => descending
                ? invitations.OrderByDescending(i => i.Status).ThenByDescending(i => i.CreatedAt)
                : invitations.OrderBy(i => i.Status).ThenBy(i => i.CreatedAt),

            "expiresat" or "expires" => descending
                ? invitations.OrderByDescending(i => i.ExpiresAt)
                : invitations.OrderBy(i => i.ExpiresAt),

            "updatedat" or "updated" => descending
                ? invitations.OrderByDescending(i => i.UpdatedAt)
                : invitations.OrderBy(i => i.UpdatedAt),

            _ => descending
                ? invitations.OrderByDescending(i => i.CreatedAt)
                : invitations.OrderBy(i => i.CreatedAt)
        };
    }
}
