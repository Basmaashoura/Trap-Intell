using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Accounts;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.CancelInvitation;

/// <summary>
/// Handler for CancelInvitationCommand.
/// Revokes the invitation and removes the pending contact.
/// </summary>
internal sealed class CancelInvitationCommandHandler : ICommandHandler<CancelInvitationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CancelInvitationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(CancelInvitationCommand command, CancellationToken cancellationToken)
    {
        // Find the invitation
        AccountContact? invitation = await _context.AccountContacts
            .FirstOrDefaultAsync(
                c => c.Id == command.InvitationId && c.AccountId == command.AccountId,
                cancellationToken);

        if (invitation is null)
        {
            return Result.Failure(AccountContactErrors.InviteNotFound);
        }

        // Cannot cancel if already accepted
        if (invitation.IsInviteAccepted)
        {
            return Result.Failure(AccountContactErrors.InviteAlreadyAccepted);
        }

        // Check if current user can cancel invitations
        Guid currentUserId = _currentUserService.UserId;

        // Check if user is staff (admin)
        User? currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);
        bool isStaffUser = currentUser?.AccountType == AccountType.Staff;

        AccountContact? currentUserContact = await _context.AccountContacts
            .FirstOrDefaultAsync(
                c => c.UserId == currentUserId &&
                     c.AccountId == command.AccountId &&
                     c.IsActive &&
                     c.IsInviteAccepted,
                cancellationToken);

        bool canManageAsContact = currentUserContact is not null &&
                                  (currentUserContact.IsPrimaryContact || currentUserContact.Permissions.CanManageContacts);

        if (!isStaffUser && !canManageAsContact)
        {
            return Result.Failure(AccountContactErrors.NotAuthorizedToManageContacts);
        }

        // Cancel the invitation (soft delete - keep record)
        invitation.CancelInvitation();
        invitation.Deactivate();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
