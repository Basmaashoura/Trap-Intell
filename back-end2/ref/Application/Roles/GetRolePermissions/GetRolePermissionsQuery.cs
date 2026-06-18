using Application.Abstractions.Messaging;
using Application.Roles.Common;

namespace Application.Roles.GetRolePermissions;

/// <summary>
/// Query to get permissions assigned to a role.
/// </summary>
/// <param name="RoleId">The ID of the role.</param>
public sealed record GetRolePermissionsQuery(Guid RoleId) : IQuery<IReadOnlyList<PermissionResponse>>;
