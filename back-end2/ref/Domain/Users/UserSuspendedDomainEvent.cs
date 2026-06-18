using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Raised when a user is suspended.
/// </summary>
public sealed record UserSuspendedDomainEvent(Guid UserId) : IDomainEvent;
