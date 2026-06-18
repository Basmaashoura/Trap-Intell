using System.Security.Cryptography;
using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Accounts.AcceptInvitation;

/// <summary>
/// Handler for AcceptInvitationCommand.
/// Validates the token and activates the contact.
/// </summary>
internal sealed class AcceptInvitationCommandHandler : ICommandHandler<AcceptInvitationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public AcceptInvitationCommandHandler(
        IApplicationDbContext context,
        IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<Result> Handle(AcceptInvitationCommand command, CancellationToken cancellationToken)
    {
        // Find the invitation
        AccountContact? invitation = await _context.AccountContacts
            .Include(c => c.Account)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == command.InvitationId, cancellationToken);

        if (invitation is null)
        {
            return Result.Failure(AccountContactErrors.InviteNotFound);
        }

        // Check if already accepted
        if (invitation.IsInviteAccepted)
        {
            return Result.Failure(AccountContactErrors.InviteAlreadyAccepted);
        }

        // Check if expired
        if (invitation.IsInvitationExpired)
        {
            return Result.Failure(AccountSettingsErrors.InviteExpired);
        }

        // Validate token
        string tokenHash = HashToken(command.Token);
        if (!invitation.ValidateInvitationToken(tokenHash))
        {
            return Result.Failure(AccountContactErrors.InviteNotFound);
        }

        // Check if this is a new user who needs to set password
        bool userHasIdentity = await _identityService.UserExistsAsync(
            invitation.UserId,
            cancellationToken);

        if (!userHasIdentity)
        {
            // New user - must provide password
            if (string.IsNullOrEmpty(command.Password))
            {
                return Result.Failure(Error.Validation(
                    "AcceptInvitation.PasswordRequired",
                    "Password is required for new users."));
            }

            // Create identity for the user
            Result<Guid> createResult = await _identityService.CreateUserAsync(
                invitation.UserId,
                invitation.User!.Email,
                command.Password,
                cancellationToken);

            if (createResult.IsFailure)
            {
                return Result.Failure(createResult.Error);
            }
        }

        // Accept the invitation
        Result acceptResult = invitation.AcceptInvitation();
        if (acceptResult.IsFailure)
        {
            return acceptResult;
        }

        // Clear the invitation token (one-time use)
        invitation.ClearInvitationToken();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static string HashToken(string token)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(token);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
