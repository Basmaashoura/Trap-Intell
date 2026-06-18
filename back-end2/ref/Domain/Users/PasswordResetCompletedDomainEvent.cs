using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Domain event raised when a user's password is reset.
/// </summary>
public sealed record PasswordResetCompletedDomainEvent(Guid UserId) : IDomainEvent;
