using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Domain.Auditing.Services;

namespace Trap_Intel.Domain.Honeypots.Services
{
    /// <summary>
    /// Background service for monitoring honeypot health.
    /// 
    /// ?? WARNING: This service violates DDD layering principles and should be moved to Application Layer.
    /// It orchestrates multiple aggregates and persistence - these are Application Layer concerns.
    /// 
    /// TODO: Move to Trap-Intel.Application/Honeypots/BackgroundServices/HoneypotHealthMonitorBackgroundService.cs
    /// 
    /// Responsibilities:
    /// - Periodically poll health status from external service
    /// - Update honeypot health metrics
    /// - Detect failures (3-strike auto-error rule)
    /// - Raise domain events for subscribers
    /// - Maintain audit trail
    /// 
    /// This service is designed to be called by a background job scheduler (Hangfire, Quartz, etc.)
    /// from the infrastructure layer.
    /// </summary>
    [Obsolete("This service should be moved to Application Layer as a background service handler.")]
    public class HoneypotHealthMonitorService
    {
        private readonly IHoneypotRepository _honeypotRepository;
        private readonly IExternalHoneypotService _externalService;
        private readonly AuditService _auditService;
        private readonly IAuditTrailRepository _auditRepository;

        public HoneypotHealthMonitorService(
            IHoneypotRepository honeypotRepository,
            IExternalHoneypotService externalService,
            AuditService auditService,
            IAuditTrailRepository auditRepository)
        {
            _honeypotRepository = honeypotRepository ?? throw new ArgumentNullException(nameof(honeypotRepository));
            _externalService = externalService ?? throw new ArgumentNullException(nameof(externalService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        }

        /// <summary>
        /// Monitor all active honeypots.
        /// 
        /// Workflow:
        /// 1. Get all active honeypots
        /// 2. For each honeypot:
        ///    a. Request health status from external service
        ///    b. Update honeypot health metrics
        ///    c. Check failure counter (3-strike rule)
        ///    d. Persist updates
        ///    e. Raise domain events
        /// 
        /// Called periodically (e.g., every 5 minutes) by background job.
        /// </summary>
        public async Task MonitorAllHoneypotsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Get all active honeypots
                var activeHoneypots = await _honeypotRepository.GetByStatusAsync(
                    HoneypotStatus.Active,
                    cancellationToken);

                if (!activeHoneypots.Any())
                    return; // No honeypots to monitor

                // Monitor each honeypot
                var updates = new List<Honeypot>();

                foreach (var honeypot in activeHoneypots)
                {
                    var updated = await CheckHoneypotHealthAsync(honeypot, cancellationToken);
                    if (updated)
                        updates.Add(honeypot);
                }

                // Batch update honeypots
                if (updates.Any())
                {
                    await _honeypotRepository.UpdateBatchAsync(updates, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // Log monitoring failure but don't crash
                // This will be logged in infrastructure error handler
                System.Diagnostics.Debug.WriteLine($"Error in honeypot health monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Monitor a specific honeypot.
        /// 
        /// Called from MonitorAllHoneypotsAsync for each honeypot.
        /// Returns true if honeypot was updated.
        /// </summary>
        private async Task<bool> CheckHoneypotHealthAsync(
            Honeypot honeypot,
            CancellationToken cancellationToken)
        {
            try
            {
                // Validate honeypot has external service link
                if (honeypot.ExternalService is null)
                {
                    var auditResult = _auditService.CreateFailureAuditLog(
                        honeypot.OrganizationId,
                        null,
                        AuditResourceType.HoneyPot,
                        honeypot.Id,
                        AuditAction.Update,
                        "Health check failed: honeypot not linked to external service");

                    if (auditResult.IsSuccess)
                    {
                        await _auditRepository.AddAsync(auditResult.Value);
                    }

                    return false;
                }

                // Request health status from external service
                var healthResult = await _externalService.GetHealthAsync(
                    honeypot.ExternalService.ServiceId,
                    cancellationToken);

                // Handle health check response
                if (healthResult.IsSuccess)
                {
                    return HandleHealthCheckSuccess(honeypot, healthResult.Value);
                }
                else
                {
                    return HandleHealthCheckFailure(honeypot, healthResult.Errors);
                }
            }
            catch (Exception ex)
            {
                // Log unexpected errors
                System.Diagnostics.Debug.WriteLine($"Unexpected error checking health for honeypot {honeypot.Id}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handle successful health check response.
        /// </summary>
        private bool HandleHealthCheckSuccess(
            Honeypot honeypot,
            ExternalServiceHealthStatus externalHealthStatus)
        {
            // Map external service health to domain health value object
            var health = new HoneypotHealth(
                status: externalHealthStatus.IsHealthy ? global::Trap_Intel.Domain.Honeypots.HoneypotHealthStatus.Healthy : global::Trap_Intel.Domain.Honeypots.HoneypotHealthStatus.Degraded,
                lastHeartbeat: externalHealthStatus.LastHeartbeat ?? DateTime.UtcNow,
                cpuUsagePercent: externalHealthStatus.CpuUsagePercent,
                memoryUsagePercent: externalHealthStatus.MemoryUsagePercent,
                diskUsagePercent: externalHealthStatus.DiskUsagePercent,
                activeConnections: externalHealthStatus.ActiveConnections,
                storageUsedBytes: externalHealthStatus.StorageUsedBytes,
                failedConnectionAttempts: externalHealthStatus.FailedConnectionAttempts);

            // Record heartbeat (this resets failure counter internally)
            var result = honeypot.RecordHeartbeat(health);

            if (result.IsSuccess)
            {
                // If honeypot was in Error status and is now healthy, transition back to Active
                if (honeypot.Status == HoneypotStatus.Error && externalHealthStatus.IsHealthy)
                {
                    var recoveryResult = honeypot.UpdateStatus(HoneypotStatus.Active, "Recovered from error state");
                    if (recoveryResult.IsFailure)
                        System.Diagnostics.Debug.WriteLine($"Failed to recover honeypot {honeypot.Id} from error state");
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Handle failed health check response.
        /// </summary>
        private bool HandleHealthCheckFailure(
            Honeypot honeypot,
            IReadOnlyList<Error> errors)
        {
            var errorMessage = string.Join(", ", errors.Select(e => e.Message));

            // Record failure (3-strike rule handled in domain model)
            var result = honeypot.RecordHealthCheckFailure(errorMessage);

            if (result.IsSuccess)
            {
                // If honeypot transitioned to Error status, log it
                if (honeypot.Status == HoneypotStatus.Error)
                {
                    // Log will be handled by AuditService callback on domain event
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Get comprehensive health report for all honeypots.
        /// 
        /// Used for dashboards and reporting.
        /// </summary>
        public async Task<HoneypotHealthReport> GetHealthReportAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var honeypots = await _honeypotRepository.GetByOrganizationAsync(
                    organizationId,
                    cancellationToken);

                var totalCount = honeypots.Count;
                var activeCount = honeypots.Count(h => h.Status == HoneypotStatus.Active);
                var pausedCount = honeypots.Count(h => h.Status == HoneypotStatus.Paused);
                var errorCount = honeypots.Count(h => h.Status == HoneypotStatus.Error);
                var healthyCount = honeypots.Count(h => h.Health.IsHealthy);
                var degradedCount = honeypots.Count(h => h.Health.IsDegraded);
                var unhealthyCount = honeypots.Count(h => h.Health.IsUnhealthy);

                var totalStorageGb = honeypots.Sum(h => h.Health.StorageUsedGb);
                var avgCpuUsage = honeypots.Any() ? honeypots.Average(h => h.Health.CpuUsagePercent) : 0;
                var avgMemoryUsage = honeypots.Any() ? honeypots.Average(h => h.Health.MemoryUsagePercent) : 0;

                return new HoneypotHealthReport(
                    OrganizationId: organizationId,
                    TotalHoneypots: totalCount,
                    ActiveHoneypots: activeCount,
                    PausedHoneypots: pausedCount,
                    ErrorHoneypots: errorCount,
                    HealthyHoneypots: healthyCount,
                    DegradedHoneypots: degradedCount,
                    UnhealthyHoneypots: unhealthyCount,
                    TotalStorageGb: totalStorageGb,
                    AverageCpuUsagePercent: avgCpuUsage,
                    AverageMemoryUsagePercent: avgMemoryUsage,
                    ReportGeneratedAt: DateTime.UtcNow,
                    Honeypots: honeypots.Select(h => new HoneypotHealthSnapshot(
                        HoneypotId: h.Id,
                        Name: h.Name,
                        Type: h.Type,
                        Status: h.Status,
                        Health: h.Health,
                        LastHeartbeat: h.LastHeartbeat,
                        StorageUsedGb: h.Health.StorageUsedGb)).ToList());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating health report: {ex.Message}");
                return new HoneypotHealthReport(
                    OrganizationId: organizationId,
                    TotalHoneypots: 0,
                    ActiveHoneypots: 0,
                    PausedHoneypots: 0,
                    ErrorHoneypots: 0,
                    HealthyHoneypots: 0,
                    DegradedHoneypots: 0,
                    UnhealthyHoneypots: 0,
                    TotalStorageGb: 0,
                    AverageCpuUsagePercent: 0,
                    AverageMemoryUsagePercent: 0,
                    ReportGeneratedAt: DateTime.UtcNow,
                    Honeypots: new List<HoneypotHealthSnapshot>());
            }
        }
    }

    /// <summary>
    /// Comprehensive health report for honeypots in an organization.
    /// </summary>
    public record HoneypotHealthReport(
        Guid OrganizationId,
        int TotalHoneypots,
        int ActiveHoneypots,
        int PausedHoneypots,
        int ErrorHoneypots,
        int HealthyHoneypots,
        int DegradedHoneypots,
        int UnhealthyHoneypots,
        decimal TotalStorageGb,
        decimal AverageCpuUsagePercent,
        decimal AverageMemoryUsagePercent,
        DateTime ReportGeneratedAt,
        IReadOnlyList<HoneypotHealthSnapshot> Honeypots);

    /// <summary>
    /// Health snapshot for a single honeypot.
    /// </summary>
    public record HoneypotHealthSnapshot(
        Guid HoneypotId,
        string Name,
        HoneypotType Type,
        HoneypotStatus Status,
        HoneypotHealth Health,
        DateTime? LastHeartbeat,
        decimal StorageUsedGb);
}
