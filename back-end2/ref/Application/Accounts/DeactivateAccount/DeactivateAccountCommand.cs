using Application.Abstractions.Messaging;

namespace Application.Accounts.DeactivateAccount;

/// <summary>
/// Command to deactivate an account.
/// </summary>
/// <param name="AccountId">The ID of the account to deactivate.</param>
public sealed record DeactivateAccountCommand(Guid AccountId) : ICommand;
