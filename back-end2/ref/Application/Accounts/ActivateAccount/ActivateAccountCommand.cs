using Application.Abstractions.Messaging;

namespace Application.Accounts.ActivateAccount;

/// <summary>
/// Command to activate a deactivated account.
/// </summary>
/// <param name="AccountId">The ID of the account to activate.</param>
public sealed record ActivateAccountCommand(Guid AccountId) : ICommand;
