using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Trap_Intel.Domain.Identity.Policies;

/// <summary>
/// Policy for user permission checks.
/// Delegates to RolePermissionMap for role-based access control.
/// </summary>
public static class UserPermissionPolicy
{
    /// <summary>
    /// Check if user has a specific permission.
    /// </summary>
    public static bool HasPermission(Guid roleId, string permissionName)
    {
        return RolePermissionMap.HasPermission(roleId, permissionName);
    }

    /// <summary>
    /// Get all permissions for a role.
    /// </summary>
    public static List<string> GetPermissionsForRole(Guid roleId)
    {
        return RolePermissionMap.GetPermissions(roleId).ToList();
    }

    /// <summary>
    /// Check if user is an administrator.
    /// </summary>
    public static bool IsAdmin(Guid roleId) =>
        roleId == Roles.SystemRoles.OrganizationAdminId || roleId == Roles.SystemRoles.SuperAdminId;

    /// <summary>
    /// Check if role can be assigned by another role.
    /// </summary>
    public static bool CanAssignRole(Guid assignerRoleId, Guid targetRoleId)
    {
        return RolePermissionMap.CanAssignRole(assignerRoleId, targetRoleId);
    }
}
