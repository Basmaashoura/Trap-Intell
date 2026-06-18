using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Honeypots.Services
{
    /// <summary>
    /// Abstraction for the external Go-based honeypot service.
    /// This interface defines the contract for communicating with the honeypot deployment service via REST APIs.
    /// 
    /// DOMAIN LAYER ONLY:
    /// - This interface lives in the domain layer (pure abstraction)
    /// - Implementation lives in infrastructure layer
    /// - DTOs live in infrastructure layer (anti-corruption layer)
    /// 
    /// Implementation: Infrastructure layer (HttpClientFactory, HttpClient)
    /// Does NOT implement: Actual honeypot logic - that's external service responsibility
    /// </summary>
    public interface IExternalHoneypotService
    {
        /// <summary>
        /// Deploy a new honeypot to the external service.
        /// 
        /// Workflow:
        /// 1. Accept domain model request
        /// 2. Infrastructure layer converts to external service DTO
        /// 3. Send HTTP POST to external service
        /// 4. Parse response
        /// 5. Convert back to domain response object
        /// 
        /// External Service Responsibility:
        /// - Allocate resources
        /// - Deploy honeypot process
        /// - Listen on configured port
        /// - Begin capturing events
        /// - Return IP and service ID
        /// </summary>
        Task<Result<ExternalHoneypotDeploymentResponse>> DeployAsync(
            ExternalHoneypotDeploymentRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Terminate a deployed honeypot.
        /// 
        /// External Service Responsibility:
        /// - Stop honeypot process
        /// - Clean up resources
        /// - Archive logs
        /// - Return final statistics
        /// </summary>
        Task<Result> TerminateAsync(
            string externalHoneypotId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get current health status of a honeypot.
        /// 
        /// Called periodically by HoneypotHealthMonitorService.
        /// 
        /// External Service Responsibility:
        /// - Collect system metrics
        /// - Count active connections
        /// - Check honeypot process status
        /// - Return health data
        /// </summary>
        Task<Result<ExternalServiceHealthStatus>> GetHealthAsync(
            string externalHoneypotId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetch captured event logs from honeypot.
        /// 
        /// Called by HoneypotLogFetchService (Phase 3).
        /// Only returns events captured since the 'since' timestamp.
        /// 
        /// External Service Responsibility:
        /// - Query stored event logs
        /// - Filter by timestamp range
        /// - Serialize events to JSON
        /// - Return paginated results
        /// </summary>
        Task<Result<IReadOnlyList<ExternalHoneypotEvent>>> GetLogsAsync(
            string externalHoneypotId,
            DateTime since,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Update honeypot configuration on the external service.
        /// 
        /// External Service Responsibility:
        /// - Apply configuration changes
        /// - Restart if necessary
        /// - Return confirmation
        /// </summary>
        Task<Result> UpdateConfigurationAsync(
            string externalHoneypotId,
            ExternalHoneypotConfigurationUpdate configUpdate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if external service is healthy and reachable.
        /// 
        /// Used to validate external service availability before deployment.
        /// </summary>
        Task<Result<bool>> IsHealthyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get detailed statistics for a honeypot.
        /// 
        /// Returns comprehensive statistics from the external service.
        /// </summary>
        Task<Result<ExternalHoneypotStatistics>> GetStatisticsAsync(
            string externalHoneypotId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Pause honeypot (stop capturing but keep deployed).
        /// 
        /// External Service Responsibility:
        /// - Stop event capture
        /// - Keep honeypot process running
        /// - Maintain port binding
        /// </summary>
        Task<Result> PauseAsync(
            string externalHoneypotId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resume honeypot (resume capturing after pause).
        /// 
        /// External Service Responsibility:
        /// - Resume event capture
        /// - Continue accepting connections
        /// </summary>
        Task<Result> ResumeAsync(
            string externalHoneypotId,
            CancellationToken cancellationToken = default);
    }

    // ============================================================================
    // IMPORTANT: The following types represent EXTERNAL SERVICE DOMAIN CONCEPTS
    // They are NOT the same as our internal domain models (Honeypot, etc.)
    // This is the ANTI-CORRUPTION LAYER boundary
    // 
    // Infrastructure layer:
    // 1. Receives request from domain service (using these types)
    // 2. Converts to external service DTO format (wire format)
    /// 3. Sends HTTP request
    /// 4. Receives HTTP response
    /// 5. Converts back to these types
    /// 6. Returns to domain service
    // ============================================================================

    /// <summary>
    /// ANTI-CORRUPTION LAYER: Represents external service deployment request.
    /// This is a domain-level abstraction of the external service's concept of "deployment request".
    /// Infrastructure layer maps between this and the actual HTTP DTO.
    /// </summary>
    public sealed record ExternalHoneypotDeploymentRequest
    {
        public required string Name { get; init; }
        public required HoneypotType Type { get; init; }
        public required string OrganizationId { get; init; }
        public required HoneypotConfiguration Configuration { get; init; }
        public Dictionary<string, string>? Metadata { get; init; }
    }

    /// <summary>
    /// ANTI-CORRUPTION LAYER: Represents external service deployment response.
    /// This is a domain-level abstraction of the external service's concept of "deployment result".
    /// </summary>
    public sealed record ExternalHoneypotDeploymentResponse
    {
        public required string ExternalHoneypotId { get; init; }
        public required string IpAddress { get; init; }
        public required int Port { get; init; }
        public required DateTime DeploymentTime { get; init; }
        public required string Status { get; init; }
        public string? ServiceVersion { get; init; }
    }

    /// <summary>
    /// ANTI-CORRUPTION LAYER: Represents external service health status response.
    /// Maps to the external service's concept of "health status".
    /// Infrastructure layer converts HTTP health response to this.
    /// This is DIFFERENT from our domain HoneypotHealth value object.
    /// </summary>
    public sealed record ExternalServiceHealthStatus
    {
        public required string ExternalHoneypotId { get; init; }
        public required bool IsHealthy { get; init; }
        public DateTime? LastHeartbeat { get; init; }
        public required string IpAddress { get; init; }
        public required int Port { get; init; }
        public required decimal CpuUsagePercent { get; init; }
        public required decimal MemoryUsagePercent { get; init; }
        public required decimal DiskUsagePercent { get; init; }
        public required long StorageUsedBytes { get; init; }
        public required int ActiveConnections { get; init; }
        public required int FailedConnectionAttempts { get; init; }
        public required string CurrentStatus { get; init; }
        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// ANTI-CORRUPTION LAYER: Represents a captured event from external honeypot.
    /// Infrastructure layer converts HTTP event DTO to this domain-level abstraction.
    /// </summary>
    public sealed record ExternalHoneypotEvent
    {
        public required Guid Id { get; init; }
        public required DateTime Timestamp { get; init; }
        public required string SourceIp { get; init; }
        public required int SourcePort { get; init; }
        public required string Action { get; init; }
        public required string Protocol { get; init; }
        public string? Payload { get; init; }
        public required string Severity { get; init; }
    }

    /// <summary>
    /// ANTI-CORRUPTION LAYER: Configuration update request for external honeypot.
    /// </summary>
    public sealed record ExternalHoneypotConfigurationUpdate
    {
        public int? Port { get; init; }
        public LogCaptureLevel? CaptureLevel { get; init; }
        public string? Credentials { get; init; }
        public int? MaxConnections { get; init; }
        public bool? RecordPayload { get; init; }
        public int? RetentionDays { get; init; }
        public Dictionary<string, string>? CustomSettings { get; init; }
    }

    /// <summary>
    /// ANTI-CORRUPTION LAYER: Comprehensive statistics from external honeypot service.
    /// Domain-level representation of external service's statistics concept.
    /// </summary>
    public sealed record ExternalHoneypotStatistics
    {
        public required string ExternalHoneypotId { get; init; }
        public required int TotalEventsCaptured { get; init; }
        public required int CriticalEvents { get; init; }
        public required int HighSeverityEvents { get; init; }
        public required int MediumSeverityEvents { get; init; }
        public required int LowSeverityEvents { get; init; }
        public required int UniqueSourceIps { get; init; }
        public required int FailedAuthenticationAttempts { get; init; }
        public required int SuccessfulConnectionAttempts { get; init; }
        public DateTime? FirstEventTime { get; init; }
        public DateTime? LastEventTime { get; init; }
        public required long TotalBytesStored { get; init; }
        public required decimal AverageEventsPerDay { get; init; }
        public DateTime? LastDataFetchTime { get; init; }
        public required bool IsRunning { get; init; }
        public required string CurrentStatus { get; init; }
    }
}
