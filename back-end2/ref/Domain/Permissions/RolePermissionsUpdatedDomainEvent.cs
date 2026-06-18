using SharedKernel;

namespace Domain.Permissions;

/// <summary>
/// Raised when a role's permissions are updated.
/// Triggers UI refresh for affected users.
/// </summary>
public sealed record RolePermissionsUpdatedDomainEvent(Guid RoleId) : IDomainEvent;
