using SharedKernel;

namespace Domain.Permissions;

public sealed record RoleCreatedDomainEvent(Guid RoleId) : IDomainEvent;
