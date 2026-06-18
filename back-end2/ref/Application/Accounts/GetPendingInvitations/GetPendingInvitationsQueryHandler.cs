using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Accounts;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.GetPendingInvitations;

/// <summary>
/// Handler for GetPendingInvitationsQuery.
/// Returns pending invitations for an account.
/// </summary>
internal sealed class GetPendingInvitationsQueryHandler 
    : IQueryHandler<GetPendingInvitationsQuery, IReadOnlyList<PendingInvitationResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetPendingInvitationsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<PendingInvitationResponse>>> Handle(
        GetPendingInvitationsQuery query,
        CancellationToken cancellationToken)
    {
        // Verify account exists
        bool accountExists = await _context.Accounts
            .AnyAsync(a => a.Id == query.AccountId, cancellationToken);

        if (!accountExists)
        {
            return Result.Failure<IReadOnlyList<PendingInvitationResponse>>(
                AccountErrors.NotFound(query.AccountId));
        }

        // Check if current user can view invitations
        Guid currentUserId = _currentUserService.UserId;

        // Check if user is staff (admin)
        User? currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);
        bool isStaffUser = currentUser?.AccountType == AccountType.Staff;

        AccountContact? currentUserContact = await _context.AccountContacts
            .FirstOrDefaultAsync(
                c => c.UserId == currentUserId &&
                     c.AccountId == query.AccountId &&
                     c.IsActive &&
                     c.IsInviteAccepted,
                cancellationToken);

        bool canViewAsContact = currentUserContact is not null &&
                                (currentUserContact.IsPrimaryContact || currentUserContact.Permissions.CanManageContacts);

        if (!isStaffUser && !canViewAsContact)
        {
            return Result.Failure<IReadOnlyList<PendingInvitationResponse>>(
                AccountContactErrors.NotAuthorizedToManageContacts);
        }


        // Get pending invitations
        DateTime now = DateTime.UtcNow;

        List<PendingInvitationResponse> invitations = await _context.AccountContacts
            .Include(c => c.User)
            .Where(c => c.AccountId == query.AccountId && !c.IsInviteAccepted && c.IsActive)
            .Select(c => new PendingInvitationResponse(
                c.Id,
                c.UserId,
                c.User!.Email,
                c.User.FirstName,
                c.User.LastName,
                c.Role,
                c.InvitedAt ?? c.CreatedAt,
                c.InviteExpiresAt,
                c.InviteExpiresAt.HasValue && c.InviteExpiresAt.Value < now,
                null)) // InvitedByName would require another join
            .OrderByDescending(i => i.InvitedAt)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PendingInvitationResponse>>(invitations);
    }
}
