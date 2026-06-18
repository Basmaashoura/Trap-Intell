using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Domain event raised when two-factor authentication is disabled for a user.
/// </summary>
public sealed record TwoFactorDisabledDomainEvent(Guid UserId) : IDomainEvent;
