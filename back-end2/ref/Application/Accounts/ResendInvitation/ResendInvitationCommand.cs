using Application.Abstractions.Messaging;

namespace Application.Accounts.ResendInvitation;

/// <summary>
/// Command to resend a contact invitation email.
/// </summary>
/// <param name="AccountId">The account ID.</param>
/// <param name="InvitationId">The invitation (contact) ID.</param>
/// <param name="ExpirationDays">New expiration days (default: 7).</param>
public sealed record ResendInvitationCommand(
    Guid AccountId,
    Guid InvitationId,
    int ExpirationDays = 7) : ICommand;
