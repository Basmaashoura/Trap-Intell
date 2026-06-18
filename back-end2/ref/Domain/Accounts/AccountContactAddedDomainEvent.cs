using SharedKernel;

namespace Domain.Accounts;

/// <summary>
/// Raised when a new contact is added to an account.
/// </summary>
public sealed record AccountContactAddedDomainEvent(
    Guid AccountId,
    Guid UserId,
    bool IsPrimaryContact) : IDomainEvent;
