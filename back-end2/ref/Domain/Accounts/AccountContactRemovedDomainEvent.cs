using SharedKernel;

namespace Domain.Accounts;

/// <summary>
/// Raised when a contact is removed from an account.
/// </summary>
public sealed record AccountContactRemovedDomainEvent(
    Guid AccountId,
    Guid UserId) : IDomainEvent;
