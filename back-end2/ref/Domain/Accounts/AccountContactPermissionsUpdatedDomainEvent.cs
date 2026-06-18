using SharedKernel;

namespace Domain.Accounts;

/// <summary>
/// Raised when a contact's permissions are updated.
/// </summary>
public sealed record AccountContactPermissionsUpdatedDomainEvent(
    Guid AccountId,
    Guid UserId) : IDomainEvent;
