using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Identity.Policies;

/// <summary>
/// Policy for user status transitions.
/// Encapsulates status state machine logic.
/// </summary>
public static class UserStatusPolicy
{
    /// <summary>
    /// Validate activation.
    /// </summary>
    public static Result ValidateActivation(UserStatus currentStatus)
    {
        if (currentStatus == UserStatus.Active)
            return Result.Failure(IdentityErrors.UserAlreadyActive);

        return Result.Success();
    }

    /// <summary>
    /// Validate deactivation.
    /// </summary>
    public static Result ValidateDeactivation(UserStatus currentStatus, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(
                Error.Custom("Identity.InvalidReason", "Deactivation reason cannot be empty."));

        if (currentStatus == UserStatus.Inactive)
            return Result.Failure(IdentityErrors.UserAlreadyInactive);

        return Result.Success();
    }

    /// <summary>
    /// Validate unsuspend.
    /// </summary>
    public static Result ValidateUnsuspend(UserStatus currentStatus)
    {
        if (currentStatus != UserStatus.Suspended)
            return Result.Failure(
                Error.Custom("Identity.UserNotSuspended", "User is not suspended."));

        return Result.Success();
    }

    /// <summary>
    /// Validate role change.
    /// </summary>
    public static Result ValidateRoleChange(System.Guid currentRoleId, System.Guid newRoleId)
    {
        if (newRoleId == currentRoleId)
            return Result.Failure(
                Error.Custom("Identity.SameRole", "New role must be different from current role."));

        return Result.Success();
    }

    /// <summary>
    /// Check if user is active.
    /// </summary>
    public static bool IsActive(UserStatus status) => status == UserStatus.Active;

    /// <summary>
    /// Check if status is terminal.
    /// </summary>
    public static bool IsTerminalStatus(UserStatus status) => 
        status == UserStatus.Inactive;

    /// <summary>
    /// Get allowed status transitions.
    /// </summary>
    public static List<UserStatus> GetAllowedTransitions(UserStatus currentStatus)
    {
        return currentStatus switch
        {
            UserStatus.PendingActivation => new List<UserStatus> { UserStatus.Active, UserStatus.Inactive },
            UserStatus.Active => new List<UserStatus> { UserStatus.Inactive, UserStatus.Suspended },
            UserStatus.Suspended => new List<UserStatus> { UserStatus.Active },
            UserStatus.Inactive => new List<UserStatus> { UserStatus.Active },
            _ => new List<UserStatus>()
        };
    }
}
