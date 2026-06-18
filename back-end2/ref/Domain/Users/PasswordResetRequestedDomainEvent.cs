using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Domain event raised when a user requests a password reset.
/// </summary>
public sealed record PasswordResetRequestedDomainEvent(
    Guid UserId,
    string Email) : IDomainEvent;
