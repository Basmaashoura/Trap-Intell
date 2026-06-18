namespace Trap_Intel.Domain.Identity.Authorization;

/// <summary>
/// Type-safe permission constants for the entire system.
/// Organized by resource domain for clarity and maintainability.
/// </summary>
public static class Permissions
{
    /// <summary>
    /// Honeypot management permissions.
    /// </summary>
    public static class Honeypots
    {
        public const string View = "honeypots:view";
        public const string Create = "honeypots:create";
        public const string Update = "honeypots:update";
        public const string Delete = "honeypots:delete";
        public const string Deploy = "honeypots:deploy";
        public const string Configure = "honeypots:configure";
    }

    /// <summary>
    /// Attack event permissions.
    /// </summary>
    public static class Attacks
    {
        public const string View = "attacks:view";
        public const string Analyze = "attacks:analyze";
        public const string Export = "attacks:export";
    }

    /// <summary>
    /// Alert management permissions.
    /// </summary>
    public static class Alerts
    {
        public const string View = "alerts:view";
        public const string Acknowledge = "alerts:acknowledge";
        public const string Escalate = "alerts:escalate";
        public const string Configure = "alerts:configure";
    }

    /// <summary>
    /// Threat actor permissions.
    /// </summary>
    public static class Threats
    {
        public const string View = "threats:view";
        public const string Analyze = "threats:analyze";
        public const string Update = "threats:update";
    }

    /// <summary>
    /// Reporting and analytics permissions.
    /// </summary>
    public static class Reports
    {
        public const string View = "reports:view";
        public const string Create = "reports:create";
        public const string Export = "reports:export";
    }

    /// <summary>
    /// Dashboard permissions.
    /// </summary>
    public static class Dashboards
    {
        public const string View = "dashboards:view";
        public const string Create = "dashboards:create";
        public const string Manage = "dashboards:manage";
    }

    /// <summary>
    /// User management permissions.
    /// </summary>
    public static class Users
    {
        public const string View = "users:view";
        public const string Create = "users:create";
        public const string Update = "users:update";
        public const string Delete = "users:delete";
        public const string ManageRoles = "users:manage-roles";
        public const string Invite = "users:invite";
    }

    /// <summary>
    /// Organization management permissions.
    /// </summary>
    public static class Organization
    {
        public const string View = "organization:view";
        public const string Update = "organization:update";
        public const string ManageSettings = "organization:manage-settings";
        public const string ManageBilling = "organization:manage-billing";
        public const string ManageApiKeys = "organization:manage-api-keys";
        public const string ManageWebhooks = "organization:manage-webhooks";
    }

    /// <summary>
    /// System-level permissions (SuperAdmin only).
    /// </summary>
    public static class System
    {
        public const string ManageGlobalSettings = "system:manage-global-settings";
        public const string ManageOrganizations = "system:manage-organizations";
        public const string ManagePlans = "system:manage-plans";
        public const string ViewAuditLogs = "system:view-audit-logs";
        public const string ManageSuperAdmins = "system:manage-super-admins";
    }

    /// <summary>
    /// Agent command permissions.
    /// </summary>
    public static class Commands
    {
        public const string View = "commands:view";
        public const string Execute = "commands:execute";
        public const string Approve = "commands:approve";
    }

    /// <summary>
    /// Gets all defined permission strings in the system.
    /// Used for validation and seeding.
    /// </summary>
    public static IReadOnlyList<string> GetAll()
    {
        return
        [
            // Honeypots
            Honeypots.View, Honeypots.Create, Honeypots.Update,
            Honeypots.Delete, Honeypots.Deploy, Honeypots.Configure,
            // Attacks
            Attacks.View, Attacks.Analyze, Attacks.Export,
            // Alerts
            Alerts.View, Alerts.Acknowledge, Alerts.Escalate, Alerts.Configure,
            // Threats
            Threats.View, Threats.Analyze, Threats.Update,
            // Reports
            Reports.View, Reports.Create, Reports.Export,
            // Dashboards
            Dashboards.View, Dashboards.Create, Dashboards.Manage,
            // Users
            Users.View, Users.Create, Users.Update,
            Users.Delete, Users.ManageRoles, Users.Invite,
            // Organization
            Organization.View, Organization.Update, Organization.ManageSettings,
            Organization.ManageBilling, Organization.ManageApiKeys, Organization.ManageWebhooks,
            // System
            System.ManageGlobalSettings, System.ManageOrganizations,
            System.ManagePlans, System.ViewAuditLogs, System.ManageSuperAdmins,
            // Commands
            Commands.View, Commands.Execute, Commands.Approve
        ];
    }
}
