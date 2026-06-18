using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Raised when a user logs in successfully.
/// </summary>
public sealed record UserLoggedInDomainEvent(
    Guid UserId,
    string? IpAddress,
    string? UserAgent) : IDomainEvent;
