using SharedKernel;

namespace Domain.Accounts;

/// <summary>
/// Settings and limits for an organization account.
/// Managed by admin to control resources per company.
/// </summary>
public sealed class AccountSettings : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The account these settings belong to.
    /// </summary>
    public Guid AccountId { get; private set; }

    /// <summary>
    /// Maximum number of employees allowed for this organization.
    /// Default is 5, can be increased by admin or plan upgrade.
    /// </summary>
    public int MaxEmployees { get; private set; } = 5;

    /// <summary>
    /// Maximum number of concurrent monitored users.
    /// 0 means unlimited (based on plan).
    /// </summary>
    public int MaxMonitoredUsers { get; private set; }

    /// <summary>
    /// Maximum storage space in MB for report exports and data.
    /// </summary>
    public long MaxStorageMb { get; private set; } = 1024; // 1GB default

    /// <summary>
    /// Whether this organization can use the monitoring dashboard.
    /// </summary>
    public bool DashboardAccessEnabled { get; private set; } = true;

    /// <summary>
    /// Whether employees can invite other employees.
    /// If false, only admin can add employees.
    /// </summary>
    public bool AllowEmployeeSelfInvite { get; private set; }

    /// <summary>
    /// Whether new employee invitations require admin approval.
    /// </summary>
    public bool RequireInviteApproval { get; private set; } = true;

    /// <summary>
    /// Default expiration days for employee invitations.
    /// 0 means never expires.
    /// </summary>
    public int InviteExpirationDays { get; private set; } = 7;

    /// <summary>
    /// Navigation property to Account.
    /// </summary>
    public Account? Account { get; init; }

    private AccountSettings()
    {
    }

    public static AccountSettings CreateDefault(Guid accountId)
    {
        return new AccountSettings
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            MaxEmployees = 5,
            MaxMonitoredUsers = 0,
            MaxStorageMb = 1024,
            DashboardAccessEnabled = true,
            AllowEmployeeSelfInvite = false,
            RequireInviteApproval = true,
            InviteExpirationDays = 7
        };
    }

    public static AccountSettings CreatePremium(Guid accountId)
    {
        return new AccountSettings
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            MaxEmployees = 50,
            MaxMonitoredUsers = 0,
            MaxStorageMb = 10240, // 10GB
            DashboardAccessEnabled = true,
            AllowEmployeeSelfInvite = true,
            RequireInviteApproval = false,
            InviteExpirationDays = 30
        };
    }

    public Result UpdateEmployeeLimit(int maxEmployees)
    {
        if (maxEmployees < 1)
        {
            return Result.Failure(AccountSettingsErrors.InvalidEmployeeLimit);
        }

        MaxEmployees = maxEmployees;
        return Result.Success();
    }

    public void UpdateStorageLimit(long maxStorageMb)
    {
        MaxStorageMb = maxStorageMb;
    }

    public void UpdateMonitoredUsersLimit(int maxMonitoredUsers)
    {
        MaxMonitoredUsers = maxMonitoredUsers;
    }

    public void EnableDashboardAccess()
    {
        DashboardAccessEnabled = true;
    }

    public void DisableDashboardAccess()
    {
        DashboardAccessEnabled = false;
    }

    public void UpdateInviteSettings(bool allowSelfInvite, bool requireApproval, int expirationDays)
    {
        AllowEmployeeSelfInvite = allowSelfInvite;
        RequireInviteApproval = requireApproval;
        InviteExpirationDays = expirationDays;
    }
}

/// <summary>
/// Errors for AccountSettings operations.
/// </summary>
public static class AccountSettingsErrors
{
    public static readonly Error InvalidEmployeeLimit = Error.Validation(
        "AccountSettings.InvalidEmployeeLimit",
        "Employee limit must be at least 1");

    public static readonly Error EmployeeLimitReached = Error.Failure(
        "AccountSettings.EmployeeLimitReached",
        "Maximum number of employees reached for this organization");

    public static readonly Error DashboardAccessDisabled = Error.Failure(
        "AccountSettings.DashboardAccessDisabled",
        "Dashboard access is disabled for this organization");

    public static readonly Error InviteExpired = Error.Failure(
        "AccountSettings.InviteExpired",
        "The employee invitation has expired");

    public static readonly Error InviteRequiresApproval = Error.Failure(
        "AccountSettings.InviteRequiresApproval",
        "Employee invitation requires admin approval");
}
