using Application.Abstractions.Messaging;

namespace Application.Accounts.GetPendingInvitations;

/// <summary>
/// Query to get pending (not yet accepted) invitations for an account.
/// </summary>
/// <param name="AccountId">The account ID.</param>
public sealed record GetPendingInvitationsQuery(Guid AccountId) : IQuery<IReadOnlyList<PendingInvitationResponse>>;

/// <summary>
/// Response for a pending invitation.
/// </summary>
public sealed record PendingInvitationResponse(
    Guid Id,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? Role,
    DateTime InvitedAt,
    DateTime? ExpiresAt,
    bool IsExpired,
    string? InvitedByName);
