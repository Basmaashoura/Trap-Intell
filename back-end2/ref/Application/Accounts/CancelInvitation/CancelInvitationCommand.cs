using Application.Abstractions.Messaging;

namespace Application.Accounts.CancelInvitation;

/// <summary>
/// Command to cancel/revoke a pending contact invitation.
/// </summary>
/// <param name="AccountId">The account ID.</param>
/// <param name="InvitationId">The invitation (contact) ID to cancel.</param>
public sealed record CancelInvitationCommand(
    Guid AccountId,
    Guid InvitationId) : ICommand;
