using Application.Abstractions.Messaging;

namespace Application.Accounts.UpdateAccountContact;

/// <summary>
/// Command to update an account contact.
/// </summary>
public sealed record UpdateAccountContactCommand(
    Guid AccountId,
    Guid ContactId,
    string? Role,
    bool IsPrimaryContact,
    bool IsDecisionMaker) : ICommand;
