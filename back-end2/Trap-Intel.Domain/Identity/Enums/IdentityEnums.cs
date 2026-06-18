namespace Trap_Intel.Domain.Identity
{
    /// <summary>
    /// Enums for the Identity domain.
    /// </summary>

    /// <summary>
    /// User status in the system.
    /// </summary>
    public enum UserStatus
    {
        Active = 0,
        Inactive = 1,
        Suspended = 2,
        Deleted = 3,
        PendingActivation = 4
    }

    /// <summary>
    /// Permission levels for fine-grained access control.
    /// </summary>
    public enum PermissionLevel
    {
        None = 0,
        Read = 1,
        Write = 2,
        Delete = 4,
        Admin = 8,
        Execute = 16
    }
}
