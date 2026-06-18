using SharedKernel;

namespace Domain.Permissions;

/// <summary>
/// Raised when a role is updated.
/// </summary>
public sealed record RoleUpdatedDomainEvent(Guid RoleId) : IDomainEvent;
