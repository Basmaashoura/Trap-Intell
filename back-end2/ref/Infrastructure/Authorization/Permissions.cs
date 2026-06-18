namespace Infrastructure.Authorization;

/// <summary>
/// Permission constants matching Domain Permission codes.
/// Use these instead of magic strings for compile-time safety.
/// </summary>
/// <remarks>
/// Permission format: MODULE:ACTION (e.g., "USERS:CREATE")
/// These must match the Permission.Code values stored in the database.
/// </remarks>
public static class Permissions
{
    /// <summary>
    /// User management permissions.
    /// </summary>
    public static class Users
    {
        public const string Create = "USERS:CREATE";
        public const string Read = "USERS:READ";
        public const string Update = "USERS:UPDATE";
        public const string Delete = "USERS:DELETE";
        public const string Deactivate = "USERS:DEACTIVATE";
        public const string Activate = "USERS:ACTIVATE";
        public const string ViewSensitive = "USERS:VIEWSENSITIVE";
    }

    /// <summary>
    /// Role management permissions.
    /// </summary>
    public static class Roles
    {
        public const string Create = "ROLES:CREATE";
        public const string Read = "ROLES:READ";
        public const string Update = "ROLES:UPDATE";
        public const string Delete = "ROLES:DELETE";
        public const string AssignPermissions = "ROLES:ASSIGNPERMISSIONS";
        public const string AssignUsers = "ROLES:ASSIGNUSERS";
    }

    /// <summary>
    /// Account management permissions.
    /// </summary>
    public static class Accounts
    {
        public const string Create = "ACCOUNTS:CREATE";
        public const string Read = "ACCOUNTS:READ";
        public const string Update = "ACCOUNTS:UPDATE";
        public const string Delete = "ACCOUNTS:DELETE";
        public const string Deactivate = "ACCOUNTS:DEACTIVATE";
        public const string ManageContacts = "ACCOUNTS:MANAGECONTACTS";
    }

    /// <summary>
    /// Stress keyboard data permissions.
    /// </summary>
    public static class StressData
    {
        public const string Create = "STRESSDATA:CREATE";
        public const string Read = "STRESSDATA:READ";
        public const string Update = "STRESSDATA:UPDATE";
        public const string Delete = "STRESSDATA:DELETE";
    }

    /// <summary>
    /// Stress analysis and results permissions.
    /// </summary>
    public static class StressAnalysis
    {
        public const string Read = "STRESSANALYSIS:READ";
        public const string Run = "STRESSANALYSIS:RUN";
        public const string Export = "STRESSANALYSIS:EXPORT";
    }

    /// <summary>
    /// Subscription plan permissions.
    /// </summary>
    public static class Plans
    {
        public const string Create = "PLANS:CREATE";
        public const string Read = "PLANS:READ";
        public const string Update = "PLANS:UPDATE";
        public const string Delete = "PLANS:DELETE";
    }

    /// <summary>
    /// Subscription management permissions.
    /// </summary>
    public static class Subscriptions
    {
        public const string Create = "SUBSCRIPTIONS:CREATE";
        public const string Read = "SUBSCRIPTIONS:READ";
        public const string Update = "SUBSCRIPTIONS:UPDATE";
        public const string Cancel = "SUBSCRIPTIONS:CANCEL";
    }

    /// <summary>
    /// Invoice permissions.
    /// </summary>
    public static class Invoices
    {
        public const string Create = "INVOICES:CREATE";
        public const string Read = "INVOICES:READ";
        public const string Update = "INVOICES:UPDATE";
    }

    /// <summary>
    /// Report and analytics permissions.
    /// </summary>
    public static class Reports
    {
        public const string View = "REPORTS:VIEW";
        public const string Export = "REPORTS:EXPORT";
        public const string ViewSensitive = "REPORTS:VIEWSENSITIVE";
    }

    /// <summary>
    /// System settings permissions.
    /// </summary>
    public static class Settings
    {
        public const string View = "SETTINGS:VIEW";
        public const string Manage = "SETTINGS:MANAGE";
        public const string ManageSecurity = "SETTINGS:MANAGESECURITY";
    }

    /// <summary>
    /// Audit log permissions.
    /// </summary>
    public static class AuditLogs
    {
        public const string View = "AUDITLOGS:VIEW";
        public const string Export = "AUDITLOGS:EXPORT";
    }

    /// <summary>
    /// Helper method to get all permission constants.
    /// Useful for seeding database or validation.
    /// </summary>
    public static IReadOnlyList<string> GetAll()
    {
        return
        [
            // Users
            Users.Create, Users.Read, Users.Update, Users.Delete,
            Users.Deactivate, Users.Activate, Users.ViewSensitive,

            // Roles
            Roles.Create, Roles.Read, Roles.Update, Roles.Delete,
            Roles.AssignPermissions, Roles.AssignUsers,

            // Accounts
            Accounts.Create, Accounts.Read, Accounts.Update, Accounts.Delete,
            Accounts.Deactivate, Accounts.ManageContacts,

            // Stress Data
            StressData.Create, StressData.Read, StressData.Update, StressData.Delete,

            // Stress Analysis
            StressAnalysis.Read, StressAnalysis.Run, StressAnalysis.Export,

            // Plans
            Plans.Create, Plans.Read, Plans.Update, Plans.Delete,

            // Subscriptions
            Subscriptions.Create, Subscriptions.Read, Subscriptions.Update, Subscriptions.Cancel,

            // Invoices
            Invoices.Create, Invoices.Read, Invoices.Update,

            // Reports
            Reports.View, Reports.Export, Reports.ViewSensitive,

            // Settings
            Settings.View, Settings.Manage, Settings.ManageSecurity,

            // Audit Logs
            AuditLogs.View, AuditLogs.Export
        ];
    }
}
