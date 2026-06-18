using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Honeypots.Policies;

/// <summary>
/// Policy object for honeypot status transitions.
/// Encapsulates status state machine logic.
/// </summary>
public class HoneypotStatusPolicy
{
    /// <summary>
    /// Validate if status transition is allowed.
    /// </summary>
    public static Result ValidateStatusTransition(
        HoneypotStatus currentStatus,
        HoneypotStatus newStatus)
    {
        if (!IsValidTransition(currentStatus, newStatus))
        {
            return Result.Failure(
                Error.Custom("HoneypotStatus.InvalidTransition",
                    $"Cannot transition from {currentStatus} to {newStatus}"));
        }

        return Result.Success();
    }

    /// <summary>
    /// Check if transition is valid.
    /// </summary>
    public static bool IsValidTransition(
        HoneypotStatus currentStatus,
        HoneypotStatus newStatus)
    {
        return currentStatus switch
        {
            HoneypotStatus.Provisioning =>
                newStatus == HoneypotStatus.Active ||
                newStatus == HoneypotStatus.Error,

            HoneypotStatus.Active =>
                newStatus == HoneypotStatus.Paused ||
                newStatus == HoneypotStatus.Error ||
                newStatus == HoneypotStatus.Terminated,

            HoneypotStatus.Paused =>
                newStatus == HoneypotStatus.Active ||
                newStatus == HoneypotStatus.Terminated,

            HoneypotStatus.Inactive =>
                newStatus == HoneypotStatus.Active ||
                newStatus == HoneypotStatus.Terminated,

            HoneypotStatus.Error =>
                newStatus == HoneypotStatus.Terminated ||
                newStatus == HoneypotStatus.Active,

            HoneypotStatus.Terminated => false,
            HoneypotStatus.Retired => false,

            _ => false
        };
    }

    /// <summary>
    /// Validate pause operation.
    /// </summary>
    public static Result ValidatePause(Honeypot honeypot)
    {
        if (honeypot.Status != HoneypotStatus.Active)
            return Result.Failure(HoneypotErrors.CannotPauseInactiveHoneypot);

        return Result.Success();
    }

    /// <summary>
    /// Validate resume operation.
    /// </summary>
    public static Result ValidateResume(Honeypot honeypot)
    {
        if (honeypot.Status != HoneypotStatus.Paused)
            return Result.Failure(HoneypotErrors.CannotResumeActiveHoneypot);

        return Result.Success();
    }

    /// <summary>
    /// Validate terminate operation.
    /// </summary>
    public static Result ValidateTerminate(Honeypot honeypot)
    {
        if (honeypot.Status == HoneypotStatus.Terminated)
            return Result.Failure(HoneypotErrors.CannotTerminateTerminatedHoneypot);

        return Result.Success();
    }

    /// <summary>
    /// Validate deployment mark.
    /// </summary>
    public static Result ValidateMarkAsDeployed(Honeypot honeypot)
    {
        if (honeypot.Status != HoneypotStatus.Provisioning)
            return Result.Failure(HoneypotErrors.InvalidStatusTransition);

        if (honeypot.ExternalService is null || honeypot.NetworkInfo is null)
            return Result.Failure(HoneypotErrors.ExternalServiceNotLinked);

        return Result.Success();
    }

    /// <summary>
    /// Validate deployment failure mark.
    /// </summary>
    public static Result ValidateMarkDeploymentFailed(
        Honeypot honeypot,
        string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return Result.Failure(
                Error.Custom("HoneypotStatus.InvalidErrorMessage",
                    "Error message cannot be empty"));

        if (honeypot.Status != HoneypotStatus.Provisioning)
            return Result.Failure(HoneypotErrors.InvalidStatusTransition);

        return Result.Success();
    }

    /// <summary>
    /// Get allowed transitions for current status.
    /// </summary>
    public static List<HoneypotStatus> GetAllowedTransitions(HoneypotStatus currentStatus)
    {
        var allowed = new List<HoneypotStatus>();

        foreach (var status in Enum.GetValues<HoneypotStatus>())
        {
            if (IsValidTransition(currentStatus, status))
            {
                allowed.Add(status);
            }
        }

        return allowed;
    }

    /// <summary>
    /// Check if status is terminal (no further transitions).
    /// </summary>
    public static bool IsTerminalStatus(HoneypotStatus status)
    {
        return status == HoneypotStatus.Terminated ||
               status == HoneypotStatus.Retired;
    }

    /// <summary>
    /// Check if status is active state.
    /// </summary>
    public static bool IsActiveState(HoneypotStatus status)
    {
        return status == HoneypotStatus.Active ||
               status == HoneypotStatus.Paused;
    }
}
