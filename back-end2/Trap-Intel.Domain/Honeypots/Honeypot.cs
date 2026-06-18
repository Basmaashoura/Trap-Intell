using System;
using System.Collections.Generic;
using System.Linq;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Honeypots
{
    /// <summary>
    /// Represents a deployed honeypot instance.
    /// Enterprise-grade honeypot management with full lifecycle support.
    /// Communicates with external Go-based honeypot service via APIs.
    /// </summary>
    public class Honeypot : AggregateRoot<Guid>
    {
        private List<string> _notes = new();
        private int _consecutiveHealthCheckFailures = 0;

        // NEW: Heartbeat monitoring fields (Phase 2A-2)
        private int _consecutiveMissedHeartbeats = 0;
        private DateTime? _lastHeartbeatAt = null;
        private DateTime? _lastOfflineAt = null;

        private Honeypot() { }

        private Honeypot(
            Guid id,
            Guid organizationId,
            Guid subscriptionId,
            string name,
            HoneypotType type,
            HoneypotConfiguration configuration,
            HoneypotDeploymentLocation deploymentLocation)
            : base(id)
        {
            OrganizationId = organizationId;
            SubscriptionId = subscriptionId;
            Name = name;
            Type = type;
            Configuration = configuration;
            DeploymentLocation = deploymentLocation;
            Status = HoneypotStatus.Provisioning;
            Health = new HoneypotHealth();
            Statistics = new HoneypotStatistics();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            LastLogFetch = null;
        }

        // Properties
        public Guid OrganizationId { get; private set; }
        public Guid SubscriptionId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public HoneypotType Type { get; private set; }
        public HoneypotStatus Status { get; private set; }
        public HoneypotConfiguration Configuration { get; private set; } = null!;
        public HoneypotDeploymentLocation DeploymentLocation { get; private set; }
        public ExternalServiceReference? ExternalService { get; private set; }
        public HoneypotNetworkInfo? NetworkInfo { get; private set; }
        public HoneypotHealth Health { get; private set; } = null!;
        public HoneypotStatistics Statistics { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public DateTime? LastHeartbeat { get; private set; }
        public DateTime? LastLogFetch { get; private set; }
        public DateTime? DeployedAt { get; private set; }
        public DateTime? TerminatedAt { get; private set; }

        public IReadOnlyList<string> Notes => _notes.AsReadOnly();

        // NEW: Heartbeat monitoring properties (Phase 2A-2)
        public DateTime? LastHeartbeatAt => _lastHeartbeatAt;
        public int ConsecutiveMissedHeartbeats => _consecutiveMissedHeartbeats;
        public HeartbeatStatus HeartbeatStatus { get; private set; } = HeartbeatStatus.Unknown;
        public bool IsConnected { get; private set; } = false;
        public string? AgentId { get; private set; }
        public string? AgentVersion { get; private set; }

        // Expose for policies
        internal int ConsecutiveHealthCheckFailures => _consecutiveHealthCheckFailures;

        #region Factory Methods

        /// <summary>
        /// Factory method to create a new honeypot.
        /// </summary>
        public static Result<Honeypot> Create(
            Guid organizationId,
            Guid subscriptionId,
            string name,
            HoneypotType type,
            HoneypotConfiguration configuration,
            HoneypotDeploymentLocation deploymentLocation = HoneypotDeploymentLocation.Cloud)
        {
            // Validation
            if (organizationId == Guid.Empty)
                return Result.Failure<Honeypot>(HoneypotErrors.InvalidOrganizationId);

            if (subscriptionId == Guid.Empty)
                return Result.Failure<Honeypot>(HoneypotErrors.InvalidSubscriptionId);

            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure<Honeypot>(HoneypotErrors.InvalidName);

            if (name.Length < 3 || name.Length > 255)
                return Result.Failure<Honeypot>(HoneypotErrors.InvalidName);

            if (configuration is null)
                return Result.Failure<Honeypot>(HoneypotErrors.InvalidConfiguration);

            var honeypot = new Honeypot(
                Guid.NewGuid(),
                organizationId,
                subscriptionId,
                name.Trim(),
                type,
                configuration,
                deploymentLocation);

            honeypot.RaiseDomainEvent(new HoneypotCreatedEvent(
                honeypot.Id,
                organizationId,
                subscriptionId,
                honeypot.Name,
                type,
                configuration.Port,
                DateTime.UtcNow));

            return Result.Success(honeypot);
        }

        /// <summary>
        /// Factory method to reconstruct honeypot from database.
        /// </summary>
        public static Honeypot Reconstruct(
            Guid id,
            Guid organizationId,
            Guid subscriptionId,
            string name,
            HoneypotType type,
            HoneypotConfiguration configuration,
            HoneypotDeploymentLocation deploymentLocation,
            HoneypotStatus status,
            ExternalServiceReference? externalService,
            HoneypotNetworkInfo? networkInfo,
            HoneypotHealth health,
            HoneypotStatistics statistics,
            DateTime createdAt,
            DateTime updatedAt,
            DateTime? lastHeartbeat = null,
            DateTime? lastLogFetch = null,
            DateTime? deployedAt = null,
            DateTime? terminatedAt = null,
            List<string>? notes = null)
        {
            var honeypot = new Honeypot(
                id,
                organizationId,
                subscriptionId,
                name,
                type,
                configuration,
                deploymentLocation)
            {
                Status = status,
                ExternalService = externalService,
                NetworkInfo = networkInfo,
                Health = health,
                Statistics = statistics,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                LastHeartbeat = lastHeartbeat,
                LastLogFetch = lastLogFetch,
                DeployedAt = deployedAt,
                TerminatedAt = terminatedAt,
                _notes = notes ?? new()
            };

            return honeypot;
        }

        #endregion

        #region Domain Operations

        /// <summary>
        /// Link honeypot to external service after deployment.
        /// </summary>
        public Result LinkExternalService(ExternalServiceReference externalService)
        {
            if (externalService is null)
                return Result.Failure(HoneypotErrors.InvalidExternalServiceId);

            if (ExternalService is not null)
                return Result.Failure(HoneypotErrors.ExternalServiceAlreadyLinked);

            ExternalService = externalService;
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        /// <summary>
        /// Update network information after deployment.
        /// </summary>
        public Result UpdateNetworkInfo(HoneypotNetworkInfo networkInfo)
        {
            if (networkInfo is null)
                return Result.Failure(HoneypotErrors.InvalidConfiguration);

            NetworkInfo = networkInfo;
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        /// <summary>
        /// Mark honeypot as deployed.
        /// </summary>
        public Result MarkAsDeployed()
        {
            if (Status != HoneypotStatus.Provisioning)
                return Result.Failure(HoneypotErrors.InvalidStatusTransition);

            if (ExternalService is null || NetworkInfo is null)
                return Result.Failure(HoneypotErrors.ExternalServiceNotLinked);

            Status = HoneypotStatus.Active;
            DeployedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new HoneypotDeployedEvent(
                Id,
                OrganizationId,
                ExternalService.ServiceId,
                NetworkInfo.IpAddress,
                NetworkInfo.Port,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Mark deployment as failed.
        /// </summary>
        public Result MarkDeploymentFailed(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                return Result.Failure(HoneypotErrors.InvalidConfiguration);

            if (Status != HoneypotStatus.Provisioning)
                return Result.Failure(HoneypotErrors.InvalidStatusTransition);

            Status = HoneypotStatus.Error;
            _notes.Add($"Deployment failed: {errorMessage}");
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new HoneypotDeploymentFailedEvent(
                Id,
                OrganizationId,
                errorMessage,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Update honeypot status.
        /// </summary>
        public Result UpdateStatus(HoneypotStatus newStatus, string? reason = null)
        {
            // Validate state transition
            if (!IsValidStatusTransition(Status, newStatus))
                return Result.Failure(HoneypotErrors.InvalidStatusTransition);

            var oldStatus = Status;
            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new HoneypotStatusChangedEvent(
                Id,
                OrganizationId,
                oldStatus,
                newStatus,
                reason,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Record heartbeat from honeypot.
        /// </summary>
        public Result RecordHeartbeat(HoneypotHealth newHealth)
        {
            if (newHealth is null)
                return Result.Failure(HoneypotErrors.InvalidConfiguration);

            if (Status == HoneypotStatus.Terminated)
                return Result.Failure(HoneypotErrors.CannotUpdateTerminatedHoneypot);

            LastHeartbeat = DateTime.UtcNow;
            Health = newHealth;
            _consecutiveHealthCheckFailures = 0;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new HoneypotHeartbeatRecordedEvent(
                Id,
                OrganizationId,
                newHealth.Status,
                newHealth.CpuUsagePercent,
                newHealth.MemoryUsagePercent,
                newHealth.ActiveConnections,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Record health check failure.
        /// </summary>
        public Result RecordHealthCheckFailure(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                return Result.Failure(HoneypotErrors.InvalidConfiguration);

            _consecutiveHealthCheckFailures++;
            UpdatedAt = DateTime.UtcNow;

            if (_consecutiveHealthCheckFailures >= 3)
            {
                Status = HoneypotStatus.Error;
                _notes.Add($"Health check failed {_consecutiveHealthCheckFailures} consecutive times: {errorMessage}");

                RaiseDomainEvent(new HoneypotHealthCheckFailedEvent(
                    Id,
                    OrganizationId,
                    errorMessage,
                    _consecutiveHealthCheckFailures,
                    DateTime.UtcNow));
            }

            return Result.Success();
        }

        /// <summary>
        /// Update captured events statistics.
        /// </summary>
        public Result UpdateEventStatistics(HoneypotStatistics newStatistics)
        {
            if (newStatistics is null)
                return Result.Failure(HoneypotErrors.InvalidConfiguration);

            Statistics = newStatistics;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new HoneypotEventsCapturedEvent(
                Id,
                OrganizationId,
                newStatistics.TotalEventsCapture,
                newStatistics.CriticalEvents,
                (long)(newStatistics.TotalEventsCapture * 1024), // Simplified storage calc
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Update storage usage.
        /// </summary>
        public Result UpdateStorageUsage(long newStorageBytes)
        {
            if (newStorageBytes < 0)
                return Result.Failure(HoneypotErrors.InvalidConfiguration);

            var oldStorage = Health.StorageUsedBytes;
            var newHealth = new HoneypotHealth(
                Health.Status,
                Health.LastHeartbeat,
                Health.CpuUsagePercent,
                Health.MemoryUsagePercent,
                Health.DiskUsagePercent,
                Health.ActiveConnections,
                newStorageBytes,
                Health.FailedConnectionAttempts);

            Health = newHealth;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new HoneypotStorageUpdatedEvent(
                Id,
                OrganizationId,
                oldStorage,
                newStorageBytes,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Update last log fetch time.
        /// </summary>
        public void UpdateLastLogFetch()
        {
            LastLogFetch = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Pause the honeypot (stop capturing but keep deployed).
        /// </summary>
        public Result Pause(string? reason = null)
        {
            if (Status != HoneypotStatus.Active)
                return Result.Failure(HoneypotErrors.CannotPauseInactiveHoneypot);

            Status = HoneypotStatus.Paused;
            UpdatedAt = DateTime.UtcNow;

            _notes.Add($"Paused: {reason ?? "No reason provided"}");

            RaiseDomainEvent(new HoneypotPausedEvent(
                Id,
                OrganizationId,
                reason,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Resume the honeypot (resume capturing).
        /// </summary>
        public Result Resume(string? reason = null)
        {
            if (Status != HoneypotStatus.Paused)
                return Result.Failure(HoneypotErrors.CannotResumeActiveHoneypot);

            Status = HoneypotStatus.Active;
            UpdatedAt = DateTime.UtcNow;
            _consecutiveHealthCheckFailures = 0;

            _notes.Add($"Resumed: {reason ?? "No reason provided"}");

            RaiseDomainEvent(new HoneypotResumedEvent(
                Id,
                OrganizationId,
                reason,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Terminate the honeypot (end of life).
        /// </summary>
        public Result Terminate(string? reason = null)
        {
            if (Status == HoneypotStatus.Terminated)
                return Result.Failure(HoneypotErrors.CannotTerminateTerminatedHoneypot);

            Status = HoneypotStatus.Terminated;
            TerminatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            _notes.Add($"Terminated: {reason ?? "No reason provided"}");

            RaiseDomainEvent(new HoneypotTerminatedEvent(
                Id,
                OrganizationId,
                SubscriptionId,
                reason,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Add note to honeypot.
        /// </summary>
        public Result AddNote(string note)
        {
            if (string.IsNullOrWhiteSpace(note))
                return Result.Failure(HoneypotErrors.InvalidNote);

            if (note.Length > 1000)
                return Result.Failure(HoneypotErrors.NoteTooLong);

            _notes.Add(note.Trim());
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        /// <summary>
        /// Update honeypot configuration with port conflict validation.
        /// </summary>
        public Result UpdateConfiguration(
            HoneypotConfiguration newConfiguration,
            IEnumerable<Honeypot> existingHoneypots)
        {
            if (newConfiguration is null)
                return Result.Failure(HoneypotErrors.InvalidConfiguration);

            if (Status == HoneypotStatus.Terminated)
                return Result.Failure(HoneypotErrors.CannotUpdateTerminatedHoneypot);

            if (Status == HoneypotStatus.Provisioning)
                return Result.Failure(HoneypotErrors.CannotUpdateDeployingHoneypot);

            // Check for port conflicts
            var activeHoneypots = existingHoneypots
                .Where(h => h.Id != this.Id && 
                            h.Status != HoneypotStatus.Terminated &&
                            h.Status != HoneypotStatus.Retired);
                    
            var hasPortConflict = activeHoneypots.Any(h => 
                h.Configuration.Port == newConfiguration.Port);
                
            if (hasPortConflict)
                return Result.Failure(
                    Error.Custom("Honeypot.PortConflict", 
                        $"Port {newConfiguration.Port} is already in use by another honeypot."));

            Configuration = newConfiguration;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new HoneypotConfigurationUpdatedEvent(
                Id,
                OrganizationId,
                "Port, CaptureLevel, RetentionDays",
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Update honeypot configuration (backward compatible overload).
        /// </summary>
        public Result UpdateConfiguration(HoneypotConfiguration newConfiguration)
        {
            return UpdateConfiguration(newConfiguration, Enumerable.Empty<Honeypot>());
        }

        #endregion

        #region Heartbeat Monitoring (Phase 2A-2) - Uses HoneypotHeartbeatPolicy

        /// <summary>
        /// Record heartbeat from Go honeypot agent.
        /// Delegates to HoneypotHeartbeatPolicy for business logic.
        /// </summary>
        public Result RecordHeartbeat(string agentId, string agentVersion)
        {
            // Delegate to policy
            var policyResult = Policies.HoneypotHeartbeatPolicy.ProcessHeartbeat(
                this, agentId, agentVersion);

            if (policyResult.IsFailure)
                return Result.Failure(policyResult.Errors[0]);

            var state = policyResult.Value;

            // Update state
            var oldStatus = HeartbeatStatus;
            _lastHeartbeatAt = DateTime.UtcNow;
            AgentId = state.AgentId;
            AgentVersion = state.AgentVersion;
            IsConnected = true;
            _consecutiveMissedHeartbeats = 0;
            HeartbeatStatus = state.NewStatus;
            UpdatedAt = DateTime.UtcNow;

            // Raise events
            RaiseDomainEvent(new HoneypotHeartbeatReceivedEvent(
                Id,
                OrganizationId,
                agentId,
                agentVersion,
                DateTime.UtcNow));

            if (oldStatus != HeartbeatStatus.Healthy)
            {
                RaiseDomainEvent(new HoneypotHeartbeatStatusChangedEvent(
                    Id,
                    OrganizationId,
                    oldStatus,
                    HeartbeatStatus.Healthy,
                    0,
                    DateTime.UtcNow));
            }

            if (state.WasOffline && state.Downtime.HasValue)
            {
                RaiseDomainEvent(new HoneypotBackOnlineEvent(
                    Id,
                    OrganizationId,
                    state.Downtime.Value,
                    DateTime.UtcNow));
            }

            return Result.Success();
        }

        /// <summary>
        /// Record missed heartbeat (called by background job).
        /// Delegates to HoneypotHeartbeatPolicy for business logic.
        /// </summary>
        public Result RecordMissedHeartbeat()
        {
            // Delegate to policy
            var policyResult = Policies.HoneypotHeartbeatPolicy.ProcessMissedHeartbeat(this);

            if (policyResult.IsFailure)
                return Result.Failure(policyResult.Errors[0]);

            var state = policyResult.Value;

            if (state.ShouldIgnore)
                return Result.Success();

            // Update state
            _consecutiveMissedHeartbeats = state.MissedCount;
            HeartbeatStatus = state.NewStatus;

            if (state.WentOffline)
            {
                IsConnected = false;
                _lastOfflineAt = DateTime.UtcNow;
                _notes.Add($"Honeypot went offline after {state.MissedCount} missed heartbeats");

                RaiseDomainEvent(new HoneypotOfflineEvent(
                    Id,
                    OrganizationId,
                    state.MissedCount,
                    _lastHeartbeatAt,
                    DateTime.UtcNow));
            }

            if (state.OldStatus != state.NewStatus)
            {
                RaiseDomainEvent(new HoneypotHeartbeatStatusChangedEvent(
                    Id,
                    OrganizationId,
                    state.OldStatus,
                    state.NewStatus,
                    state.MissedCount,
                    DateTime.UtcNow));
            }

            UpdatedAt = DateTime.UtcNow;
            return Result.Success();
        }

        /// <summary>
        /// Check if honeypot is offline.
        /// Delegates to HoneypotHeartbeatPolicy.
        /// </summary>
        public bool IsOffline() => 
            Policies.HoneypotHeartbeatPolicy.IsOffline(this);

        /// <summary>
        /// Get time since last heartbeat.
        /// Delegates to HoneypotHeartbeatPolicy.
        /// </summary>
        public TimeSpan? GetTimeSinceLastHeartbeat() => 
            Policies.HoneypotHeartbeatPolicy.GetTimeSinceLastHeartbeat(this);

        /// <summary>
        /// Check if heartbeat is overdue.
        /// Delegates to HoneypotHeartbeatPolicy.
        /// </summary>
        public bool IsHeartbeatOverdue() => 
            Policies.HoneypotHeartbeatPolicy.IsHeartbeatOverdue(this);

        #endregion

        #region Storage Management - Uses HoneypotStoragePolicy

        /// <summary>
        /// Check if storage usage is approaching the quota limit.
        /// Delegates to HoneypotStoragePolicy.
        /// </summary>
        public bool IsStorageNearLimit(decimal quotaGb, decimal warningThresholdPercent = 80) =>
            Policies.HoneypotStoragePolicy.IsStorageNearLimit(this, quotaGb, warningThresholdPercent);

        /// <summary>
        /// Get storage usage percentage.
        /// Delegates to HoneypotStoragePolicy.
        /// </summary>
        public decimal GetStorageUsagePercent(decimal quotaGb) =>
            Policies.HoneypotStoragePolicy.CalculateStorageUsagePercent(this, quotaGb);

        /// <summary>
        /// Get remaining storage available.
        /// Delegates to HoneypotStoragePolicy.
        /// </summary>
        public decimal GetRemainingStorageGb(decimal quotaGb) =>
            Policies.HoneypotStoragePolicy.CalculateRemainingStorageGb(this, quotaGb);

        /// <summary>
        /// Get comprehensive storage status.
        /// Delegates to HoneypotStoragePolicy.
        /// </summary>
        public Policies.StorageStatus GetStorageStatus(decimal quotaGb) =>
            Policies.HoneypotStoragePolicy.GetStorageStatus(this, quotaGb);

        #endregion

        #region Private Validation Methods - Uses HoneypotStatusPolicy

        /// <summary>
        /// Validate if status transition is allowed.
        /// Delegates to HoneypotStatusPolicy.
        /// </summary>
        private static bool IsValidStatusTransition(HoneypotStatus currentStatus, HoneypotStatus newStatus) =>
            Policies.HoneypotStatusPolicy.IsValidTransition(currentStatus, newStatus);

        #endregion
    }
}
