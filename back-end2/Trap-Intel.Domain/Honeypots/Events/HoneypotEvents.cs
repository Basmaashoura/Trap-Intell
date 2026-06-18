using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Honeypots
{
    /// <summary>
    /// Domain events for the Honeypots domain.
    /// </summary>

    /// <summary>
    /// Raised when a new honeypot is created.
    /// </summary>
    public record HoneypotCreatedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        Guid SubscriptionId,
        string Name,
        HoneypotType Type,
        int Port,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when a honeypot deployment is requested to external service.
    /// </summary>
    public record HoneypotDeploymentRequestedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        Guid SubscriptionId,
        HoneypotType Type,
        string ExternalServiceId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when honeypot deployment to external service completes successfully.
    /// </summary>
    public record HoneypotDeployedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        string ExternalServiceId,
        string IpAddress,
        int Port,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when honeypot deployment fails.
    /// </summary>
    public record HoneypotDeploymentFailedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        string ErrorMessage,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when honeypot status changes.
    /// </summary>
    public record HoneypotStatusChangedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        HoneypotStatus OldStatus,
        HoneypotStatus NewStatus,
        string? Reason,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when honeypot heartbeat is recorded.
    /// </summary>
    public record HoneypotHeartbeatRecordedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        HoneypotHealthStatus HealthStatus,
        decimal CpuUsagePercent,
        decimal MemoryUsagePercent,
        int ActiveConnections,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when honeypot health check fails.
    /// </summary>
    public record HoneypotHealthCheckFailedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        string ErrorMessage,
        int ConsecutiveFailures,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when events are captured by honeypot.
    /// </summary>
    public record HoneypotEventsCapturedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        int EventCount,
        int CriticalEventsCount,
        long StorageUsedBytes,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when honeypot storage usage is updated.
    /// </summary>
    public record HoneypotStorageUpdatedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        long PreviousStorageBytes,
        long CurrentStorageBytes,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when honeypot configuration is updated.
    /// </summary>
    public record HoneypotConfigurationUpdatedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        string UpdatedFields,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when honeypot is paused.
    /// </summary>
    public record HoneypotPausedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        string? Reason,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when honeypot is resumed.
    /// </summary>
    public record HoneypotResumedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        string? Reason,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when honeypot is terminated.
    /// </summary>
    public record HoneypotTerminatedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        Guid SubscriptionId,
        string? Reason,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when honeypot exceeds storage quota.
    /// </summary>
    public record HoneypotStorageQuotaExceededEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        Guid SubscriptionId,
        decimal QuotaGb,
        decimal CurrentUsageGb,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when honeypot configuration validation fails.
    /// </summary>
    public record HoneypotConfigurationValidationFailedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        string ValidationError,
        DateTime OccurredOn) : IDomainEvent;

    // NEW: Heartbeat Monitoring Events (Phase 2A-2)

    /// <summary>
    /// Raised when heartbeat is received from Go honeypot agent.
    /// </summary>
    public record HoneypotHeartbeatReceivedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        string AgentId,
        string AgentVersion,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when honeypot goes offline (missed heartbeats).
    /// </summary>
    public record HoneypotOfflineEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        int ConsecutiveMissedHeartbeats,
        DateTime? LastHeartbeatAt,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when offline honeypot comes back online.
    /// </summary>
    public record HoneypotBackOnlineEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        TimeSpan DowntimeDuration,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when heartbeat status changes (healthy ? warning ? critical).
    /// </summary>
    public record HoneypotHeartbeatStatusChangedEvent(
        Guid HoneypotId,
        Guid OrganizationId,
        HeartbeatStatus OldStatus,
        HeartbeatStatus NewStatus,
        int MissedHeartbeats,
        DateTime OccurredOn) : IDomainEvent;
}
