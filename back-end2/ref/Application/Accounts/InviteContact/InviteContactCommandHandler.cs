using System.Security.Cryptography;
using Application.Abstractions.Data;
using Application.Abstractions.Email;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Accounts;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.InviteContact;

/// <summary>
/// Handler for InviteContactCommand.
/// Creates a user (if needed) and sends an invitation email.
/// </summary>
internal sealed class InviteContactCommandHandler : ICommandHandler<InviteContactCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUserService;

    public InviteContactCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _emailService = emailService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(InviteContactCommand command, CancellationToken cancellationToken)
    {
        // Verify account exists
        Account? account = await _context.Accounts
            .Include(a => a.Contacts)
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure<Guid>(AccountErrors.NotFound(command.AccountId));
        }

        // Check if current user can invite contacts
        Guid currentUserId = _currentUserService.UserId;
        
        // Get current user to check if they're staff (admin)
        User? currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        bool isStaffUser = currentUser?.AccountType == AccountType.Staff;

        // Check if user is a contact with permission
        AccountContact? inviter = account.Contacts
            .FirstOrDefault(c => c.UserId == currentUserId && c.IsActive && c.IsInviteAccepted);

        bool canInviteAsContact = inviter is not null &&
                                  (inviter.IsPrimaryContact || inviter.Permissions.CanManageContacts);

        // Staff users (admins) can always invite, or contacts with permission
        bool canInvite = isStaffUser || canInviteAsContact;

        if (!canInvite)
        {
            return Result.Failure<Guid>(AccountContactErrors.CannotInviteContacts);
        }

        // Check if user already exists with this email
        User? existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        User user;
        bool isNewUser = false;

        if (existingUser is not null)
        {
            // Check if already a contact of this account
            if (account.HasContact(existingUser.Id))
            {
                return Result.Failure<Guid>(AccountContactErrors.AlreadyContact);
            }

            user = existingUser;
        }
        else
        {
            // Create new user with EndUser type
            user = User.Create(
                command.Email,
                command.FirstName,
                command.LastName,
                string.Empty, // Password will be set when accepting invitation
                AccountType.EndUser);

            _context.Users.Add(user);
            isNewUser = true;
        }

        // Create MINIMAL permissions - Primary contact will set proper permissions later
        var permissions = ContactPermissions.CreateMinimal();

        // Create invitation
        var contact = AccountContact.CreateInvitation(
            command.AccountId,
            user.Id,
            currentUserId,
            command.Role,
            permissions,
            command.ExpirationDays);

        // Generate secure invitation token
        string token = GenerateSecureToken();
        string tokenHash = HashToken(token);
        contact.SetInvitationToken(tokenHash);

        _context.AccountContacts.Add(contact);
        await _context.SaveChangesAsync(cancellationToken);

        // Send invitation email
        await _emailService.SendContactInvitationAsync(
            command.Email,
            command.FirstName,
            account.Name,
            inviter?.User?.FirstName ?? "Admin",
            token,
            contact.Id,
            isNewUser,
            cancellationToken);

        return Result.Success(contact.Id);
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
