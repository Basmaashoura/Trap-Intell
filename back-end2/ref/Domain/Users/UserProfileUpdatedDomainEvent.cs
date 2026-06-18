using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Raised when a user's profile is updated.
/// </summary>
public sealed record UserProfileUpdatedDomainEvent(Guid UserId) : IDomainEvent;
