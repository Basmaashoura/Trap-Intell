using Application.Abstractions.Messaging;

namespace Application.Accounts.DeleteAccount;

/// <summary>
/// Command to delete an account.
/// </summary>
/// <param name="AccountId">The ID of the account to delete.</param>
public sealed record DeleteAccountCommand(Guid AccountId) : ICommand;
