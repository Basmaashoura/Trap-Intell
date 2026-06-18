using Application.Abstractions.Messaging;

namespace Application.Accounts.InviteContact;

/// <summary>
/// Command to invite a new contact to an account via email.
/// Creates a pending invitation that the user must accept.
/// Contact is created with MINIMAL permissions - Primary contact should
/// set proper permissions after the invitation is accepted.
/// </summary>
/// <param name="AccountId">The account to invite the contact to.</param>
/// <param name="Email">The email address to send the invitation to.</param>
/// <param name="FirstName">The contact's first name.</param>
/// <param name="LastName">The contact's last name.</param>
/// <param name="Role">The contact's role at the company (e.g., "CTO").</param>
/// <param name="ExpirationDays">Days until invitation expires (default: 7).</param>
public sealed record InviteContactCommand(
    Guid AccountId,
    string Email,
    string FirstName,
    string LastName,
    string? Role,
    int ExpirationDays = 7) : ICommand<Guid>;
