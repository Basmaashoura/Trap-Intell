using SharedKernel;

namespace Domain.Permissions;

/// <summary>
/// Raised when a role is deactivated.
/// </summary>
public sealed record RoleDeactivatedDomainEvent(Guid RoleId) : IDomainEvent;
