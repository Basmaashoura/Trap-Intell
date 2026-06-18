using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Raised when a user is deactivated (Offboarding Protocol).
/// Triggers: session invalidation, task reclaiming, squad removal.
/// </summary>
public sealed record UserDeactivatedDomainEvent(Guid UserId) : IDomainEvent;
