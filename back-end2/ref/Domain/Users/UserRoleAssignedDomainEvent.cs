using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Raised when a role is assigned to a user.
/// </summary>
public sealed record UserRoleAssignedDomainEvent(Guid UserId, Guid RoleId) : IDomainEvent;
