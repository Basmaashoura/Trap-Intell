using Application.Abstractions.Messaging;

namespace Application.Roles.UpdateRolePermissions;

/// <summary>
/// Command to update permissions assigned to a role.
/// Replaces all existing permissions with the new set.
/// </summary>
/// <param name="RoleId">The ID of the role.</param>
/// <param name="PermissionIds">The list of permission IDs to assign.</param>
public sealed record UpdateRolePermissionsCommand(
    Guid RoleId,
    IReadOnlyList<Guid> PermissionIds) : ICommand;
