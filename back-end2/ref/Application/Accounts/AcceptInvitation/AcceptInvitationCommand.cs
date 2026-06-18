using Application.Abstractions.Messaging;

namespace Application.Accounts.AcceptInvitation;

/// <summary>
/// Command to accept an account contact invitation.
/// For new users, this also sets their password.
/// </summary>
/// <param name="InvitationId">The ID of the invitation (AccountContact.Id).</param>
/// <param name="Token">The invitation token from the email.</param>
/// <param name="Password">Password for new users (null for existing users).</param>
public sealed record AcceptInvitationCommand(
    Guid InvitationId,
    string Token,
    string? Password = null) : ICommand;
