namespace Domain.Users;

/// <summary>
/// Defines the type of user account in the system.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Internal staff member (admin/analyst).
    /// Has access to internal tools and analytics dashboard.
    /// </summary>
    Staff = 1,

    /// <summary>
    /// End user being monitored for stress via desktop agent.
    /// Can view their own stress data and suggestions.
    /// </summary>
    EndUser = 2
}
