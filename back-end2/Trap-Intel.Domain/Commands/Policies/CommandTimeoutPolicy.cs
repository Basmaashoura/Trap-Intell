using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Commands.Enums;
using Trap_Intel.Domain.Commands.ValueObjects;

namespace Trap_Intel.Domain.Commands.Policies;

/// <summary>
/// Policy for command timeout and retry logic.
/// </summary>
public static class CommandTimeoutPolicy
{
    /// <summary>
    /// Check if command has timed out.
    /// </summary>
    public static bool HasTimedOut(DateTime? timeoutAt, AgentCommandStatus status)
    {
        if (!timeoutAt.HasValue)
            return false;

        if (CommandStatusPolicy.IsTerminal(status))
            return false;

        return DateTime.UtcNow > timeoutAt.Value;
    }

    /// <summary>
    /// Check if command is ready to execute (scheduled time reached).
    /// </summary>
    public static bool IsReadyToExecute(DateTime? scheduledFor)
    {
        if (!scheduledFor.HasValue)
            return true;

        return DateTime.UtcNow >= scheduledFor.Value;
    }

    /// <summary>
    /// Calculate timeout deadline.
    /// </summary>
    public static DateTime CalculateTimeoutDeadline(CommandTimeout timeout)
    {
        return DateTime.UtcNow.Add(timeout.Timeout);
    }

    /// <summary>
    /// Calculate execution time.
    /// </summary>
    public static TimeSpan? CalculateExecutionTime(DateTime? startedAt, DateTime? completedAt)
    {
        if (!startedAt.HasValue || !completedAt.HasValue)
            return null;

        return completedAt.Value - startedAt.Value;
    }

    /// <summary>
    /// Get command age.
    /// </summary>
    public static TimeSpan GetAge(DateTime createdAt)
    {
        return DateTime.UtcNow - createdAt;
    }

    /// <summary>
    /// Validate schedule time.
    /// </summary>
    public static Result ValidateSchedule(DateTime scheduledFor, AgentCommandStatus currentStatus)
    {
        if (scheduledFor <= DateTime.UtcNow)
            return Result.Failure(
                Error.Custom("AgentCommand.InvalidSchedule", "Scheduled time must be in the future"));

        if (currentStatus != AgentCommandStatus.Pending)
            return Result.Failure(CommandErrors.AlreadySent);

        return Result.Success();
    }

    /// <summary>
    /// Get timeout for command type.
    /// </summary>
    public static CommandTimeout GetRecommendedTimeout(AgentCommandType commandType)
    {
        return commandType switch
        {
            // Security operations - fast response needed
            AgentCommandType.BlockIP or 
            AgentCommandType.UnblockIP or 
            AgentCommandType.BlockIPRange => CommandTimeout.Short,

            // Agent control - may take longer
            AgentCommandType.RestartAgent or 
            AgentCommandType.UpdateAgent => CommandTimeout.Long,

            // Critical operations - fast with more retries
            AgentCommandType.StopAgent => CommandTimeout.Critical,

            // Diagnostics - can take a while
            AgentCommandType.RunDiagnostics or 
            AgentCommandType.GenerateReport => CommandTimeout.Long,

            // Default for everything else
            _ => CommandTimeout.Default
        };
    }

    /// <summary>
    /// Get priority for command type.
    /// </summary>
    public static CommandPriority GetRecommendedPriority(AgentCommandType commandType)
    {
        return commandType switch
        {
            // Critical priority
            AgentCommandType.StopAgent or 
            AgentCommandType.RestartAgent => CommandPriority.Critical,

            // High priority (security)
            AgentCommandType.BlockIP or 
            AgentCommandType.UnblockIP or 
            AgentCommandType.BlockIPRange => CommandPriority.High,

            // Low priority (background)
            AgentCommandType.RunDiagnostics or 
            AgentCommandType.CollectMetrics or 
            AgentCommandType.GenerateReport => CommandPriority.Low,

            // Normal for everything else
            _ => CommandPriority.Normal
        };
    }

    /// <summary>
    /// Check if command should auto-retry on timeout.
    /// </summary>
    public static bool ShouldAutoRetry(
        AgentCommandType commandType,
        int currentRetryCount,
        int maxRetries)
    {
        // Don't retry if max reached
        if (currentRetryCount >= maxRetries)
            return false;

        // Some commands should not be retried
        var noRetryCommands = new[]
        {
            AgentCommandType.StopAgent,
            AgentCommandType.RestartAgent
        };

        return !noRetryCommands.Contains(commandType);
    }
}
