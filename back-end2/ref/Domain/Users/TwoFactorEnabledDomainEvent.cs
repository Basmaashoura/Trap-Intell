using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Domain event raised when two-factor authentication is enabled for a user.
/// </summary>
public sealed record TwoFactorEnabledDomainEvent(Guid UserId) : IDomainEvent;
