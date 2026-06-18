using SharedKernel;

namespace Domain.Accounts;

/// <summary>
/// Raised when an account is deactivated.
/// </summary>
public sealed record AccountDeactivatedDomainEvent(Guid AccountId) : IDomainEvent;
