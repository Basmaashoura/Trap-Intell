using Application.Abstractions.Messaging;

namespace Application.Accounts.UpdateAccount;

/// <summary>
/// Command to update an existing account.
/// </summary>
public sealed record UpdateAccountCommand(
    Guid AccountId,
    string Name,
    string? Industry,
    string? Website,
    string? Phone,
    string? Address,
    string? TaxNumber) : ICommand;
