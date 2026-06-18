namespace Domain.Users;

/// <summary>
/// Represents the current status of a user account.
/// Used for the Secure Offboarding Protocol.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// User is active and can access the system.
    /// </summary>
    Active = 1,

    /// <summary>
    /// User has been deactivated (offboarding).
    /// Cannot login, all sessions invalidated.
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// User is temporarily suspended.
    /// Can be reactivated by admin.
    /// </summary>
    Suspended = 3
}
