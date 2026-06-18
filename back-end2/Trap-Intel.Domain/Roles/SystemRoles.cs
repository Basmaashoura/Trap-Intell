using System;

namespace Trap_Intel.Domain.Roles;

/// <summary>
/// Static definitions for native system roles. Used to seed the database 
/// and replace the old UserRole enum.
/// </summary>
public static class SystemRoles
{
    public static readonly Guid SuperAdminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid OrganizationAdminId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid SecurityAnalystId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    public static readonly Guid OperationsAnalystId = Guid.Parse("00000000-0000-0000-0000-000000000004");
    public static readonly Guid ViewerId = Guid.Parse("00000000-0000-0000-0000-000000000005");
    public static readonly Guid GuestId = Guid.Parse("00000000-0000-0000-0000-000000000006");

    public static string GetName(Guid roleId) => roleId switch
    {
        var id when id == SuperAdminId => "SuperAdmin",
        var id when id == OrganizationAdminId => "OrganizationAdmin",
        var id when id == SecurityAnalystId => "SecurityAnalyst",
        var id when id == OperationsAnalystId => "OperationsAnalyst",
        var id when id == ViewerId => "Viewer",
        var id when id == GuestId => "Guest",
        _ => "CustomRole"
    };

    public static bool TryResolveRoleId(string? roleValue, out Guid roleId)
    {
        if (Guid.TryParse(roleValue, out roleId))
        {
            return true;
        }

        return TryGetSystemRoleId(roleValue, out roleId);
    }

    public static bool IsSuperAdmin(string? roleValue)
    {
        return TryResolveRoleId(roleValue, out var roleId) && roleId == SuperAdminId;
    }

    public static bool TryGetSystemRoleId(string? roleName, out Guid roleId)
    {
        roleId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(roleName))
        {
            return false;
        }

        var normalized = roleName.Trim().Replace(" ", string.Empty);

        if (normalized.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            roleId = SuperAdminId;
            return true;
        }

        if (normalized.Equals("OrganizationAdmin", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            roleId = OrganizationAdminId;
            return true;
        }

        if (normalized.Equals("SecurityAnalyst", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Analyst", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Member", StringComparison.OrdinalIgnoreCase))
        {
            roleId = SecurityAnalystId;
            return true;
        }

        if (normalized.Equals("OperationsAnalyst", StringComparison.OrdinalIgnoreCase))
        {
            roleId = OperationsAnalystId;
            return true;
        }

        if (normalized.Equals("Viewer", StringComparison.OrdinalIgnoreCase))
        {
            roleId = ViewerId;
            return true;
        }

        if (normalized.Equals("Guest", StringComparison.OrdinalIgnoreCase))
        {
            roleId = GuestId;
            return true;
        }

        return false;
    }
}
