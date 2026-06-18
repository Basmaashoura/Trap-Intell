using Application.Abstractions.Messaging;

namespace Application.Accounts.CreateAccount;

/// <summary>
/// Command to create a new account.
/// </summary>
/// <param name="Name">Company/Organization name (required).</param>
/// <param name="Industry">Company industry or business type.</param>
/// <param name="Website">Company website URL.</param>
/// <param name="Phone">Primary phone number.</param>
/// <param name="Address">Company address.</param>
/// <param name="TaxNumber">Tax identification number.</param>
public sealed record CreateAccountCommand(
    string Name,
    string? Industry,
    string? Website,
    string? Phone,
    string? Address,
    string? TaxNumber) : ICommand<Guid>;
