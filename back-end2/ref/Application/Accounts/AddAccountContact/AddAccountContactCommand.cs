using Application.Abstractions.Messaging;

namespace Application.Accounts.AddAccountContact;

/// <summary>
/// Command to add a contact to an account.
/// </summary>
/// <param name="AccountId">The ID of the account.</param>
/// <param name="UserId">The ID of the user to add as contact.</param>
/// <param name="Role">The contact's role at the company.</param>
/// <param name="IsPrimaryContact">Whether this is the primary contact.</param>
/// <param name="IsDecisionMaker">Whether this contact is a decision maker.</param>
public sealed record AddAccountContactCommand(
    Guid AccountId,
    Guid UserId,
    string? Role,
    bool IsPrimaryContact = false,
    bool IsDecisionMaker = false) : ICommand<Guid>;
