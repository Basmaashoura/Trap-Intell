using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Domain event raised when a user's password is changed.
/// Triggers token revocation for security.
/// </summary>
public sealed record PasswordChangedDomainEvent(Guid UserId) : IDomainEvent;
