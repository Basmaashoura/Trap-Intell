using Application.Abstractions.Messaging;

namespace Application.Accounts.RemoveAccountContact;

/// <summary>
/// Command to remove a contact from an account.
/// </summary>
/// <param name="AccountId">The ID of the account.</param>
/// <param name="ContactId">The ID of the contact to remove.</param>
public sealed record RemoveAccountContactCommand(Guid AccountId, Guid ContactId) : ICommand;
