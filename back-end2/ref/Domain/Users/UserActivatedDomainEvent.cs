using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Raised when a user is reactivated.
/// </summary>
public sealed record UserActivatedDomainEvent(Guid UserId) : IDomainEvent;
