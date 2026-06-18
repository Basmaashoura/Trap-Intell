using Trap_Intel.Domain.Roles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Trap_Intel.Domain.Identity.Authorization;

/// <summary>
/// Maps each Guid ID to its allowed permissions.
/// Single source of truth for base role-based access control.
/// </summary>
public static class RolePermissionMap
{
    private static readonly FrozenLookup _lookup = new();

    /// <summary>
    /// Gets all permissions granted to the specified role.
    /// </summary>
    public static IReadOnlyList<string> GetPermissions(Guid roleId)
    {
        return _lookup.GetPermissions(roleId);
    }

    /// <summary>
    /// Checks if the specified role has the given permission.
    /// </summary>
    public static bool HasPermission(Guid roleId, string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return false;

        // SuperAdmin always has all permissions
        if (roleId == SystemRoles.SuperAdminId)
            return true;

        return _lookup.HasPermission(roleId, permission);
    }

    /// <summary>
    /// Checks if a role can assign another role.
    /// </summary>
    public static bool CanAssignRole(Guid assignerRoleId, Guid targetRoleId)
    {
        if (assignerRoleId == SystemRoles.SuperAdminId)
            return true;

        if (assignerRoleId == SystemRoles.OrganizationAdminId)
            return targetRoleId != SystemRoles.SuperAdminId;

        return false;
    }

    /// <summary>
    /// Gets the role hierarchy level (higher = more privileged).
    /// </summary>
    public static int GetRoleHierarchy(Guid roleId)
    {
        if (roleId == SystemRoles.SuperAdminId) return 100;
        if (roleId == SystemRoles.OrganizationAdminId) return 80;
        if (roleId == SystemRoles.SecurityAnalystId) return 60;
        if (roleId == SystemRoles.OperationsAnalystId) return 40;
        if (roleId == SystemRoles.ViewerId) return 20;
        if (roleId == SystemRoles.GuestId) return 10;
        return 0;
    }

    /// <summary>
    /// Pre-computed, immutable permission sets per system role.
    /// </summary>
    private sealed class FrozenLookup
    {
        private readonly Dictionary<Guid, HashSet<string>> _permissionSets;
        private readonly Dictionary<Guid, List<string>> _permissionLists;

        public FrozenLookup()
        {
            _permissionSets = new Dictionary<Guid, HashSet<string>>();
            _permissionLists = new Dictionary<Guid, List<string>>();

            Register(SystemRoles.SuperAdminId, Permissions.GetAll());
            Register(SystemRoles.OrganizationAdminId, BuildOrganizationAdminPermissions());
            Register(SystemRoles.SecurityAnalystId, BuildSecurityAnalystPermissions());
            Register(SystemRoles.OperationsAnalystId, BuildOperationsAnalystPermissions());
            Register(SystemRoles.ViewerId, BuildViewerPermissions());
            Register(SystemRoles.GuestId, BuildGuestPermissions());
        }

        public bool HasPermission(Guid roleId, string permission)
        {
            return _permissionSets.TryGetValue(roleId, out var set) && set.Contains(permission);
        }

        public IReadOnlyList<string> GetPermissions(Guid roleId)
        {
            return _permissionLists.TryGetValue(roleId, out var list)
                ? list
                : [];
        }

        private void Register(Guid roleId, IEnumerable<string> permissions)
        {
            var list = permissions.ToList();
            _permissionSets[roleId] = new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
            _permissionLists[roleId] = list;
        }

        private static List<string> BuildOrganizationAdminPermissions() =>
        [
            // Honeypots - full control
            Permissions.Honeypots.View, Permissions.Honeypots.Create,
            Permissions.Honeypots.Update, Permissions.Honeypots.Delete,
            Permissions.Honeypots.Deploy, Permissions.Honeypots.Configure,
            // Attacks
            Permissions.Attacks.View, Permissions.Attacks.Analyze, Permissions.Attacks.Export,
            // Alerts
            Permissions.Alerts.View, Permissions.Alerts.Acknowledge,
            Permissions.Alerts.Escalate, Permissions.Alerts.Configure,
            // Threats
            Permissions.Threats.View, Permissions.Threats.Analyze, Permissions.Threats.Update,
            // Reports
            Permissions.Reports.View, Permissions.Reports.Create, Permissions.Reports.Export,
            // Dashboards
            Permissions.Dashboards.View, Permissions.Dashboards.Create, Permissions.Dashboards.Manage,
            // Users - manage within organization
            Permissions.Users.View, Permissions.Users.Create, Permissions.Users.Update,
            Permissions.Users.Delete, Permissions.Users.ManageRoles, Permissions.Users.Invite,
            // Organization
            Permissions.Organization.View, Permissions.Organization.Update,
            Permissions.Organization.ManageSettings, Permissions.Organization.ManageBilling,
            Permissions.Organization.ManageApiKeys, Permissions.Organization.ManageWebhooks,
            // Commands
            Permissions.Commands.View, Permissions.Commands.Execute, Permissions.Commands.Approve
        ];

        private static List<string> BuildSecurityAnalystPermissions() =>
        [
            // Honeypots - create and manage
            Permissions.Honeypots.View, Permissions.Honeypots.Create,
            Permissions.Honeypots.Update, Permissions.Honeypots.Delete,
            Permissions.Honeypots.Deploy, Permissions.Honeypots.Configure,
            // Attacks - full analysis
            Permissions.Attacks.View, Permissions.Attacks.Analyze, Permissions.Attacks.Export,
            // Alerts - respond
            Permissions.Alerts.View, Permissions.Alerts.Acknowledge, Permissions.Alerts.Escalate,
            // Threats - analyze and update
            Permissions.Threats.View, Permissions.Threats.Analyze, Permissions.Threats.Update,
            // Reports
            Permissions.Reports.View, Permissions.Reports.Create, Permissions.Reports.Export,
            // Dashboards
            Permissions.Dashboards.View, Permissions.Dashboards.Create,
            // Commands
            Permissions.Commands.View, Permissions.Commands.Execute
        ];

        private static List<string> BuildOperationsAnalystPermissions() =>
        [
            // Honeypots - view and monitor
            Permissions.Honeypots.View,
            // Attacks - view
            Permissions.Attacks.View, Permissions.Attacks.Export,
            // Alerts - view and acknowledge
            Permissions.Alerts.View, Permissions.Alerts.Acknowledge,
            // Threats - view
            Permissions.Threats.View,
            // Reports
            Permissions.Reports.View, Permissions.Reports.Create,
            // Dashboards
            Permissions.Dashboards.View, Permissions.Dashboards.Create,
            // Commands - view only
            Permissions.Commands.View
        ];

        private static List<string> BuildViewerPermissions() =>
        [
            Permissions.Honeypots.View,
            Permissions.Attacks.View,
            Permissions.Alerts.View,
            Permissions.Threats.View,
            Permissions.Reports.View,
            Permissions.Dashboards.View
        ];

        private static List<string> BuildGuestPermissions() =>
        [
            Permissions.Dashboards.View
        ];
    }
}

