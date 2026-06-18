using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Raised when a role is removed from a user.
/// </summary>
public sealed record UserRoleRemovedDomainEvent(Guid UserId, Guid RoleId) : IDomainEvent;
