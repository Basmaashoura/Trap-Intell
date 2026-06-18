using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Commands.Enums;

namespace Trap_Intel.Domain.Commands.Policies;

/// <summary>
/// Policy for command status transitions.
/// Encapsulates status state machine logic.
/// </summary>
public static class CommandStatusPolicy
{
    /// <summary>
    /// Validate send operation.
    /// </summary>
    public static Result ValidateSend(AgentCommandStatus currentStatus)
    {
        if (currentStatus != AgentCommandStatus.Pending && 
            currentStatus != AgentCommandStatus.Queued)
            return Result.Failure(CommandErrors.AlreadySent);

        return Result.Success();
    }

    /// <summary>
    /// Validate acknowledge operation.
    /// </summary>
    public static Result ValidateAcknowledge(AgentCommandStatus currentStatus)
    {
        if (currentStatus != AgentCommandStatus.Sent)
            return Result.Failure(CommandErrors.NotSent);

        return Result.Success();
    }

    /// <summary>
    /// Validate execution start.
    /// </summary>
    public static Result ValidateExecutionStart(AgentCommandStatus currentStatus)
    {
        if (currentStatus != AgentCommandStatus.Acknowledged)
            return Result.Failure(CommandErrors.NotSent);

        return Result.Success();
    }

    /// <summary>
    /// Validate completion.
    /// </summary>
    public static Result ValidateCompletion(AgentCommandStatus currentStatus)
    {
        if (currentStatus == AgentCommandStatus.Completed)
            return Result.Failure(CommandErrors.AlreadyCompleted);

        if (currentStatus == AgentCommandStatus.Cancelled)
            return Result.Failure(CommandErrors.AlreadyCancelled);

        return Result.Success();
    }

    /// <summary>
    /// Validate failure marking.
    /// </summary>
    public static Result ValidateFailure(AgentCommandStatus currentStatus)
    {
        if (currentStatus == AgentCommandStatus.Completed)
            return Result.Failure(CommandErrors.AlreadyCompleted);

        return Result.Success();
    }

    /// <summary>
    /// Validate cancellation.
    /// </summary>
    public static Result ValidateCancellation(AgentCommandStatus currentStatus)
    {
        if (currentStatus == AgentCommandStatus.Completed)
            return Result.Failure(CommandErrors.CannotCancelCompleted);

        if (currentStatus == AgentCommandStatus.Cancelled)
            return Result.Failure(CommandErrors.AlreadyCancelled);

        return Result.Success();
    }

    /// <summary>
    /// Validate retry.
    /// </summary>
    public static Result ValidateRetry(AgentCommandStatus currentStatus, int retryCount, int maxRetries)
    {
        if (currentStatus == AgentCommandStatus.Completed)
            return Result.Failure(CommandErrors.CannotRetryCompleted);

        if (retryCount >= maxRetries)
            return Result.Failure(CommandErrors.MaxRetriesExceeded);

        return Result.Success();
    }

    /// <summary>
    /// Check if command is pending.
    /// </summary>
    public static bool IsPending(AgentCommandStatus status) =>
        status == AgentCommandStatus.Pending || status == AgentCommandStatus.Queued;

    /// <summary>
    /// Check if command is in progress.
    /// </summary>
    public static bool IsInProgress(AgentCommandStatus status) =>
        status == AgentCommandStatus.Sent ||
        status == AgentCommandStatus.Acknowledged ||
        status == AgentCommandStatus.InProgress;

    /// <summary>
    /// Check if command is terminal.
    /// </summary>
    public static bool IsTerminal(AgentCommandStatus status) =>
        status == AgentCommandStatus.Completed ||
        status == AgentCommandStatus.Failed ||
        status == AgentCommandStatus.Cancelled ||
        status == AgentCommandStatus.Timeout;

    /// <summary>
    /// Get allowed status transitions.
    /// </summary>
    public static List<AgentCommandStatus> GetAllowedTransitions(AgentCommandStatus currentStatus)
    {
        return currentStatus switch
        {
            AgentCommandStatus.Pending => new List<AgentCommandStatus> 
            { 
                AgentCommandStatus.Queued, 
                AgentCommandStatus.Sent, 
                AgentCommandStatus.Cancelled 
            },
            AgentCommandStatus.Queued => new List<AgentCommandStatus> 
            { 
                AgentCommandStatus.Sent, 
                AgentCommandStatus.Cancelled 
            },
            AgentCommandStatus.Sent => new List<AgentCommandStatus> 
            { 
                AgentCommandStatus.Acknowledged, 
                AgentCommandStatus.Timeout, 
                AgentCommandStatus.Failed 
            },
            AgentCommandStatus.Acknowledged => new List<AgentCommandStatus> 
            { 
                AgentCommandStatus.InProgress, 
                AgentCommandStatus.Failed 
            },
            AgentCommandStatus.InProgress => new List<AgentCommandStatus> 
            { 
                AgentCommandStatus.Completed, 
                AgentCommandStatus.Failed, 
                AgentCommandStatus.PartialSuccess 
            },
            _ => new List<AgentCommandStatus>()
        };
    }
}
