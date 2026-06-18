using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Commands.Enums;
using Trap_Intel.Domain.Commands.Events;
using Trap_Intel.Domain.Commands.ValueObjects;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Commands;

/// <summary>
/// Command sent to Go honeypot agent for execution.
/// Implements command & control pattern for bi-directional communication.
/// Tracks full lifecycle: created ? sent ? acknowledged ? executed ? completed/failed.
/// </summary>
public class AgentCommand : AggregateRoot<Guid>
{
    // Private constructor for EF
    private AgentCommand() { }

    private AgentCommand(
        Guid id,
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId,
        AgentCommandType commandType,
        CommandPayload payload,
        CommandPriority priority,
        CommandTimeout timeout)
        : base(id)
    {
        HoneypotId = honeypotId;
        OrganizationId = organizationId;
        IssuedByUserId = issuedByUserId;
        CommandType = commandType;
        Payload = payload;
        Priority = priority;
        Timeout = timeout;
        Status = AgentCommandStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        RetryCount = 0;
    }

    #region Properties

    /// <summary>
    /// Target honeypot for this command
    /// </summary>
    public Guid HoneypotId { get; private set; }

    /// <summary>
    /// Organization that owns the honeypot
    /// </summary>
    public Guid OrganizationId { get; private set; }

    /// <summary>
    /// User who issued the command
    /// </summary>
    public Guid IssuedByUserId { get; private set; }

    /// <summary>
    /// Type of command
    /// </summary>
    public AgentCommandType CommandType { get; private set; }

    /// <summary>
    /// Command payload (JSON)
    /// </summary>
    public CommandPayload Payload { get; private set; } = null!;

    /// <summary>
    /// Execution priority
    /// </summary>
    public CommandPriority Priority { get; private set; }

    /// <summary>
    /// Timeout configuration
    /// </summary>
    public CommandTimeout Timeout { get; private set; } = null!;

    /// <summary>
    /// Current status
    /// </summary>
    public AgentCommandStatus Status { get; private set; }

    /// <summary>
    /// Delivery method
    /// </summary>
    public CommandDeliveryMethod DeliveryMethod { get; private set; } = CommandDeliveryMethod.Immediate;

    /// <summary>
    /// Result from agent (after execution)
    /// </summary>
    public CommandResult? ExecutionResult { get; private set; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// When command was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When command was sent to agent
    /// </summary>
    public DateTime? SentAt { get; private set; }

    /// <summary>
    /// When agent acknowledged receipt
    /// </summary>
    public DateTime? AcknowledgedAt { get; private set; }

    /// <summary>
    /// When agent started execution
    /// </summary>
    public DateTime? ExecutionStartedAt { get; private set; }

    /// <summary>
    /// When command completed/failed
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Scheduled execution time (if delivery method is Scheduled)
    /// </summary>
    public DateTime? ScheduledFor { get; private set; }

    /// <summary>
    /// Timeout deadline
    /// </summary>
    public DateTime? TimeoutAt { get; private set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create a new command.
    /// Use AgentCommandFactory for specialized commands (BlockIP, Restart, etc.)
    /// </summary>
    public static Result<AgentCommand> Create(
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId,
        AgentCommandType commandType,
        CommandPayload payload,
        CommandPriority priority = CommandPriority.Normal,
        CommandTimeout? timeout = null)
    {
        // Validation - delegate to policy
        var validationResult = Policies.CommandValidationPolicy.ValidateCreate(
            honeypotId, organizationId, issuedByUserId, commandType, payload);
        
        if (validationResult.IsFailure)
            return Result.Failure<AgentCommand>(validationResult.Errors[0]);

        // Create command
        var command = new AgentCommand(
            Guid.NewGuid(),
            honeypotId,
            organizationId,
            issuedByUserId,
            commandType,
            payload,
            priority,
            timeout ?? CommandTimeout.Default);

        // Raise event
        command.RaiseDomainEvent(new AgentCommandCreatedEvent(
            command.Id,
            honeypotId,
            organizationId,
            commandType,
            priority,
            DateTime.UtcNow));

        return Result.Success(command);
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static AgentCommand Reconstruct(
        Guid id,
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId,
        AgentCommandType commandType,
        CommandPayload payload,
        CommandPriority priority,
        CommandTimeout timeout,
        AgentCommandStatus status,
        CommandDeliveryMethod deliveryMethod,
        CommandResult? result,
        string? errorMessage,
        int retryCount,
        DateTime createdAt,
        DateTime? sentAt,
        DateTime? acknowledgedAt,
        DateTime? executionStartedAt,
        DateTime? completedAt,
        DateTime? scheduledFor,
        DateTime? timeoutAt)
    {
        var command = new AgentCommand(
            id,
            honeypotId,
            organizationId,
            issuedByUserId,
            commandType,
            payload,
            priority,
            timeout)
        {
            Status = status,
            DeliveryMethod = deliveryMethod,
            ExecutionResult = result,
            ErrorMessage = errorMessage,
            RetryCount = retryCount,
            CreatedAt = createdAt,
            SentAt = sentAt,
            AcknowledgedAt = acknowledgedAt,
            ExecutionStartedAt = executionStartedAt,
            CompletedAt = completedAt,
            ScheduledFor = scheduledFor,
            TimeoutAt = timeoutAt
        };

        return command;
    }

    #endregion

    #region Domain Behaviors - Uses CommandStatusPolicy for validation

    /// <summary>
    /// Mark as sent to agent.
    /// Delegates validation to CommandStatusPolicy.
    /// </summary>
    public Result MarkAsSent()
    {
        var validation = Policies.CommandStatusPolicy.ValidateSend(Status);
        if (validation.IsFailure)
            return validation;

        Status = AgentCommandStatus.Sent;
        SentAt = DateTime.UtcNow;
        TimeoutAt = DateTime.UtcNow.Add(Timeout.Timeout);

        RaiseDomainEvent(new AgentCommandSentEvent(
            Id,
            HoneypotId,
            OrganizationId,
            CommandType,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Mark as acknowledged by agent.
    /// Delegates validation to CommandStatusPolicy.
    /// </summary>
    public Result MarkAsAcknowledged()
    {
        var validation = Policies.CommandStatusPolicy.ValidateAcknowledge(Status);
        if (validation.IsFailure)
            return validation;

        Status = AgentCommandStatus.Acknowledged;
        AcknowledgedAt = DateTime.UtcNow;

        RaiseDomainEvent(new AgentCommandAcknowledgedEvent(
            Id,
            HoneypotId,
            OrganizationId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Mark execution as started.
    /// Delegates validation to CommandStatusPolicy.
    /// </summary>
    public Result MarkExecutionStarted()
    {
        var validation = Policies.CommandStatusPolicy.ValidateExecutionStart(Status);
        if (validation.IsFailure)
            return validation;

        Status = AgentCommandStatus.InProgress;
        ExecutionStartedAt = DateTime.UtcNow;

        RaiseDomainEvent(new AgentCommandExecutionStartedEvent(
            Id,
            HoneypotId,
            OrganizationId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Mark as completed successfully.
    /// Delegates validation to CommandStatusPolicy.
    /// </summary>
    public Result MarkAsCompleted(CommandResult result)
    {
        if (result == null)
            return Result.Failure(CommandErrors.InvalidResult);

        var validation = Policies.CommandStatusPolicy.ValidateCompletion(Status);
        if (validation.IsFailure)
            return validation;

        Status = AgentCommandStatus.Completed;
        ExecutionResult = result;
        CompletedAt = DateTime.UtcNow;

        var duration = ExecutionStartedAt.HasValue
            ? DateTime.UtcNow - ExecutionStartedAt.Value
            : TimeSpan.Zero;

        RaiseDomainEvent(new AgentCommandCompletedEvent(
            Id,
            HoneypotId,
            OrganizationId,
            CommandType,
            result.Message,
            duration,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Mark as failed.
    /// Delegates validation to CommandStatusPolicy.
    /// </summary>
    public Result MarkAsFailed(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return Result.Failure(CommandErrors.InvalidResult);

        var validation = Policies.CommandStatusPolicy.ValidateFailure(Status);
        if (validation.IsFailure)
            return validation;

        Status = AgentCommandStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new AgentCommandFailedEvent(
            Id,
            HoneypotId,
            OrganizationId,
            CommandType,
            errorMessage,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Mark as timed out.
    /// Delegates validation to CommandStatusPolicy.
    /// </summary>
    public Result MarkAsTimedOut()
    {
        var validation = Policies.CommandStatusPolicy.ValidateFailure(Status);
        if (validation.IsFailure)
            return validation;

        Status = AgentCommandStatus.Timeout;
        ErrorMessage = $"Command timed out after {Timeout.Timeout.TotalSeconds} seconds";
        CompletedAt = DateTime.UtcNow;

        var timeoutDuration = TimeoutAt.HasValue
            ? DateTime.UtcNow - TimeoutAt.Value
            : Timeout.Timeout;

        RaiseDomainEvent(new AgentCommandTimedOutEvent(
            Id,
            HoneypotId,
            OrganizationId,
            CommandType,
            timeoutDuration,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Cancel command before execution.
    /// Delegates validation to CommandStatusPolicy.
    /// </summary>
    public Result Cancel(string reason)
    {
        var validation = Policies.CommandStatusPolicy.ValidateCancellation(Status);
        if (validation.IsFailure)
            return validation;

        Status = AgentCommandStatus.Cancelled;
        ErrorMessage = reason;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new AgentCommandCancelledEvent(
            Id,
            HoneypotId,
            OrganizationId,
            reason,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Retry command execution.
    /// Delegates validation to CommandStatusPolicy.
    /// </summary>
    public Result Retry()
    {
        var validation = Policies.CommandStatusPolicy.ValidateRetry(Status, RetryCount, Timeout.MaxRetries);
        if (validation.IsFailure)
            return validation;

        RetryCount++;
        Status = AgentCommandStatus.Pending;
        SentAt = null;
        AcknowledgedAt = null;
        ExecutionStartedAt = null;
        ErrorMessage = null;

        RaiseDomainEvent(new AgentCommandRetryAttemptedEvent(
            Id,
            HoneypotId,
            OrganizationId,
            RetryCount,
            Timeout.MaxRetries,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Schedule command for later execution.
    /// Delegates validation to CommandTimeoutPolicy.
    /// </summary>
    public Result Schedule(DateTime scheduledFor)
    {
        var validation = Policies.CommandTimeoutPolicy.ValidateSchedule(scheduledFor, Status);
        if (validation.IsFailure)
            return validation;

        ScheduledFor = scheduledFor;
        DeliveryMethod = CommandDeliveryMethod.Scheduled;
        Status = AgentCommandStatus.Queued;

        RaiseDomainEvent(new AgentCommandScheduledEvent(
            Id,
            HoneypotId,
            OrganizationId,
            CommandType,
            scheduledFor,
            DateTime.UtcNow));

        return Result.Success();
    }

    #endregion

    #region Query Helpers - Uses CommandStatusPolicy and CommandTimeoutPolicy

    /// <summary>
    /// Check if command is pending delivery.
    /// Delegates to CommandStatusPolicy.
    /// </summary>
    public bool IsPending() => Policies.CommandStatusPolicy.IsPending(Status);

    /// <summary>
    /// Check if command is in progress.
    /// Delegates to CommandStatusPolicy.
    /// </summary>
    public bool IsInProgress() => Policies.CommandStatusPolicy.IsInProgress(Status);

    /// <summary>
    /// Check if command is terminal.
    /// Delegates to CommandStatusPolicy.
    /// </summary>
    public bool IsTerminal() => Policies.CommandStatusPolicy.IsTerminal(Status);

    /// <summary>
    /// Check if command has timed out.
    /// Delegates to CommandTimeoutPolicy.
    /// </summary>
    public bool HasTimedOut() => Policies.CommandTimeoutPolicy.HasTimedOut(TimeoutAt, Status);

    /// <summary>
    /// Check if command is ready to execute.
    /// Delegates to CommandTimeoutPolicy.
    /// </summary>
    public bool IsReadyToExecute() => Policies.CommandTimeoutPolicy.IsReadyToExecute(ScheduledFor);

    /// <summary>
    /// Get total execution time.
    /// Delegates to CommandTimeoutPolicy.
    /// </summary>
    public TimeSpan? GetExecutionTime() => 
        Policies.CommandTimeoutPolicy.CalculateExecutionTime(ExecutionStartedAt, CompletedAt);

    /// <summary>
    /// Get age of command.
    /// Delegates to CommandTimeoutPolicy.
    /// </summary>
    public TimeSpan GetAge() => Policies.CommandTimeoutPolicy.GetAge(CreatedAt);

    /// <summary>
    /// Get allowed status transitions.
    /// Delegates to CommandStatusPolicy.
    /// </summary>
    public List<AgentCommandStatus> GetAllowedTransitions() =>
        Policies.CommandStatusPolicy.GetAllowedTransitions(Status);

    #endregion
}
