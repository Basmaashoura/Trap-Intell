namespace Domain.AuditLogs;

/// <summary>
/// Types of audit actions that can be logged.
/// </summary>
public enum AuditAction
{
    // Authentication
    Login = 1,
    Logout = 2,
    LoginFailed = 3,
    PasswordChanged = 4,
    PasswordReset = 5,
    TwoFactorEnabled = 6,
    TwoFactorDisabled = 7,
    SessionRevoked = 8,
    AllSessionsRevoked = 9,

    // User Management
    UserCreated = 10,
    UserUpdated = 11,
    UserDeleted = 12,
    UserActivated = 13,
    UserDeactivated = 14,
    UserSuspended = 15,
    UserRoleAssigned = 16,
    UserRoleRemoved = 17,
    ProfileUpdated = 18,

    // Role & Permission Management
    RoleCreated = 20,
    RoleUpdated = 21,
    RoleDeleted = 22,
    RoleActivated = 23,
    RoleDeactivated = 24,
    RolePermissionsUpdated = 25,

    // Account Management
    AccountCreated = 30,
    AccountUpdated = 31,
    AccountDeleted = 32,
    AccountActivated = 33,
    AccountDeactivated = 34,
    ContactAdded = 35,
    ContactUpdated = 36,
    ContactRemoved = 37,

    // Data Access
    SensitiveDataViewed = 50,
    DataExported = 51,
    ReportGenerated = 52,

    // System
    SettingsChanged = 60,
    SystemError = 70
}
