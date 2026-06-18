using System.Security.Cryptography;
using Application.Abstractions.Data;
using Application.Abstractions.Email;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Accounts;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.ResendInvitation;

/// <summary>
/// Handler for ResendInvitationCommand.
/// Generates a new token and resends the invitation email.
/// </summary>
internal sealed class ResendInvitationCommandHandler : ICommandHandler<ResendInvitationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUserService;

    public ResendInvitationCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _emailService = emailService;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(ResendInvitationCommand command, CancellationToken cancellationToken)
    {
        // Find the invitation
        AccountContact? invitation = await _context.AccountContacts
            .Include(c => c.Account)
            .Include(c => c.User)
            .FirstOrDefaultAsync(
                c => c.Id == command.InvitationId && c.AccountId == command.AccountId,
                cancellationToken);

        if (invitation is null)
        {
            return Result.Failure(AccountContactErrors.InviteNotFound);
        }

        // Cannot resend if already accepted
        if (invitation.IsInviteAccepted)
        {
            return Result.Failure(AccountContactErrors.InviteAlreadyAccepted);
        }

        // Check if current user can resend invitations
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


        // Generate new token
        string token = GenerateSecureToken();
        string tokenHash = HashToken(token);

        // Refresh the invitation
        invitation.RefreshInvitation(tokenHash, command.ExpirationDays);

        await _context.SaveChangesAsync(cancellationToken);

        // Resend email
        await _emailService.SendContactInvitationAsync(
            invitation.User!.Email,
            invitation.User.FirstName,
            invitation.Account!.Name,
            currentUserContact?.User?.FirstName ?? "Admin",
            token,
            invitation.Id,
            false, // Existing invitation
            cancellationToken);

        return Result.Success();
    }

    private static string GenerateSecureToken()
    {
        byte[] tokenBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }

    private static string HashToken(string token)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(token);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
