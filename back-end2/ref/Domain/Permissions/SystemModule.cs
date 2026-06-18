namespace Domain.Permissions;

/// <summary>
/// Defines the system modules that can have permissions.
/// Used in the Permission Matrix.
/// </summary>
public enum SystemModule
{
    // IAM Module
    Users = 1,
    Roles = 2,
    Accounts = 3,

    // Stress Detection Module
    StressData = 10,
    StressAnalysis = 11,

    // Subscription Module
    Plans = 20,
    Subscriptions = 21,
    Invoices = 22,

    // Reports Module
    Dashboard = 40,
    Reports = 41
}
