using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Commands.Enums;

namespace Trap_Intel.Domain.Commands.Events;

/// <summary>
/// Command created and queued for delivery.
/// </summary>
public record AgentCommandCreatedEvent(
    Guid CommandId,
    Guid HoneypotId,
    Guid OrganizationId,
    AgentCommandType CommandType,
    CommandPriority Priority,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Command sent to Go honeypot agent.
/// </summary>
public record AgentCommandSentEvent(
    Guid CommandId,
    Guid HoneypotId,
    Guid OrganizationId,
    AgentCommandType CommandType,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Command acknowledged by Go honeypot agent.
/// </summary>
public record AgentCommandAcknowledgedEvent(
    Guid CommandId,
    Guid HoneypotId,
    Guid OrganizationId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Command execution started by agent.
/// </summary>
public record AgentCommandExecutionStartedEvent(
    Guid CommandId,
    Guid HoneypotId,
    Guid OrganizationId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Command completed successfully.
/// </summary>
public record AgentCommandCompletedEvent(
    Guid CommandId,
    Guid HoneypotId,
    Guid OrganizationId,
    AgentCommandType CommandType,
    string ResultMessage,
    TimeSpan ExecutionDuration,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Command execution failed.
/// </summary>
public record AgentCommandFailedEvent(
    Guid CommandId,
    Guid HoneypotId,
    Guid OrganizationId,
    AgentCommandType CommandType,
    string ErrorMessage,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Command timed out waiting for response.
/// </summary>
public record AgentCommandTimedOutEvent(
    Guid CommandId,
    Guid HoneypotId,
    Guid OrganizationId,
    AgentCommandType CommandType,
    TimeSpan TimeoutDuration,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Command cancelled before execution.
/// </summary>
public record AgentCommandCancelledEvent(
    Guid CommandId,
    Guid HoneypotId,
    Guid OrganizationId,
    string Reason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Command retry attempted.
/// </summary>
public record AgentCommandRetryAttemptedEvent(
    Guid CommandId,
    Guid HoneypotId,
    Guid OrganizationId,
    int RetryAttempt,
    int MaxRetries,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Command scheduled for later execution.
/// </summary>
public record AgentCommandScheduledEvent(
    Guid CommandId,
    Guid HoneypotId,
    Guid OrganizationId,
    AgentCommandType CommandType,
    DateTime ScheduledFor,
    DateTime OccurredOn) : IDomainEvent;
