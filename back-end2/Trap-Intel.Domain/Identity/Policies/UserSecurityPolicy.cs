using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Identity.Policies;

/// <summary>
/// Policy for user login security.
/// Encapsulates brute force protection and lockout logic.
/// </summary>
public static class UserSecurityPolicy
{
    private const int MAX_FAILED_ATTEMPTS = 5;
    private const int LOCKOUT_DURATION_MINUTES = 30;

    /// <summary>
    /// Process failed login attempt.
    /// </summary>
    public static FailedLoginState ProcessFailedLogin(int currentFailedAttempts)
    {
        var newCount = currentFailedAttempts + 1;
        var shouldLockout = newCount >= MAX_FAILED_ATTEMPTS;

        return new FailedLoginState
        {
            NewFailedCount = newCount,
            ShouldLockout = shouldLockout,
            LockoutReason = shouldLockout 
                ? "Too many failed login attempts (brute force protection)" 
                : null,
            AttemptsRemaining = Math.Max(0, MAX_FAILED_ATTEMPTS - newCount)
        };
    }

    /// <summary>
    /// Check if user can be suspended.
    /// </summary>
    public static Result ValidateSuspension(Guid roleId, UserStatus currentStatus)
    {
        if (currentStatus == UserStatus.Suspended)
            return Result.Failure(IdentityErrors.UserAlreadySuspended);

        if (roleId == Roles.SystemRoles.SuperAdminId)
            return Result.Failure(IdentityErrors.UserCannotBeSuspended);

        return Result.Success();
    }

    /// <summary>
    /// Validate status transition.
    /// </summary>
    public static Result ValidateStatusTransition(
        UserStatus currentStatus,
        UserStatus newStatus)
    {
        var isValid = (currentStatus, newStatus) switch
        {
            (UserStatus.PendingActivation, UserStatus.Active) => true,
            (UserStatus.PendingActivation, UserStatus.Inactive) => true,
            (UserStatus.Active, UserStatus.Inactive) => true,
            (UserStatus.Active, UserStatus.Suspended) => true,
            (UserStatus.Suspended, UserStatus.Active) => true,
            (UserStatus.Inactive, UserStatus.Active) => true,
            _ => false
        };

        if (!isValid)
            return Result.Failure(
                Error.Custom("Identity.InvalidStatusTransition",
                    $"Cannot transition from {currentStatus} to {newStatus}"));

        return Result.Success();
    }

    /// <summary>
    /// Calculate lockout remaining time.
    /// </summary>
    public static TimeSpan? GetLockoutRemainingTime(DateTime? lockoutStartTime)
    {
        if (!lockoutStartTime.HasValue)
            return null;

        var lockoutEnd = lockoutStartTime.Value.AddMinutes(LOCKOUT_DURATION_MINUTES);
        var remaining = lockoutEnd - DateTime.UtcNow;

        return remaining > TimeSpan.Zero ? remaining : null;
    }

    /// <summary>
    /// Check if lockout has expired.
    /// </summary>
    public static bool IsLockoutExpired(DateTime? lockoutStartTime)
    {
        if (!lockoutStartTime.HasValue)
            return true;

        return DateTime.UtcNow >= lockoutStartTime.Value.AddMinutes(LOCKOUT_DURATION_MINUTES);
    }

    /// <summary>
    /// Get max failed attempts allowed.
    /// </summary>
    public static int GetMaxFailedAttempts() => MAX_FAILED_ATTEMPTS;
}

/// <summary>
/// State object for failed login processing.
/// </summary>
public class FailedLoginState
{
    public int NewFailedCount { get; set; }
    public bool ShouldLockout { get; set; }
    public string? LockoutReason { get; set; }
    public int AttemptsRemaining { get; set; }
}
