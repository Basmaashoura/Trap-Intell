using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Honeypots.Policies;

/// <summary>
/// Policy object for honeypot health monitoring.
/// Encapsulates health check and failure logic.
/// </summary>
public class HoneypotHealthPolicy
{
    private const int MAX_CONSECUTIVE_FAILURES = 3;

    /// <summary>
    /// Process health check failure.
    /// </summary>
    public static Result<HealthCheckFailureState> ProcessHealthCheckFailure(
        Honeypot honeypot,
        string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return Result.Failure<HealthCheckFailureState>(
                Error.Custom("HoneypotHealth.InvalidErrorMessage",
                    "Error message cannot be empty"));

        // This accesses private field _consecutiveHealthCheckFailures
        // We'll need to add a property or method to expose this
        var newFailureCount = honeypot.ConsecutiveHealthCheckFailures + 1;
        bool shouldMarkAsError = newFailureCount >= MAX_CONSECUTIVE_FAILURES;

        var state = new HealthCheckFailureState
        {
            FailureCount = newFailureCount,
            ErrorMessage = errorMessage,
            ShouldMarkAsError = shouldMarkAsError
        };

        return Result.Success(state);
    }

    /// <summary>
    /// Validate health update.
    /// </summary>
    public static Result ValidateHealthUpdate(
        Honeypot honeypot,
        HoneypotHealth newHealth)
    {
        if (newHealth is null)
            return Result.Failure(
                Error.Custom("HoneypotHealth.InvalidHealth",
                    "Health object cannot be null"));

        if (honeypot.Status == HoneypotStatus.Terminated)
            return Result.Failure(HoneypotErrors.CannotUpdateTerminatedHoneypot);

        return Result.Success();
    }

    /// <summary>
    /// Determine health status based on metrics.
    /// </summary>
    public static HoneypotHealthStatus DetermineHealthStatus(HoneypotHealth health)
    {
        // Critical if any metric is critical
        if (health.CpuUsagePercent >= 90 ||
            health.MemoryUsagePercent >= 90 ||
            health.DiskUsagePercent >= 95)
        {
            return HoneypotHealthStatus.Unhealthy;
        }

        // Degraded if metrics are high
        if (health.CpuUsagePercent >= 70 ||
            health.MemoryUsagePercent >= 70 ||
            health.DiskUsagePercent >= 80)
        {
            return HoneypotHealthStatus.Degraded;
        }

        return HoneypotHealthStatus.Healthy;
    }

    /// <summary>
    /// Get health summary with recommendations.
    /// </summary>
    public static HealthSummary GetHealthSummary(Honeypot honeypot)
    {
        var health = honeypot.Health;
        var issues = new List<string>();
        var recommendations = new List<string>();

        // Check CPU
        if (health.CpuUsagePercent >= 90)
        {
            issues.Add($"Critical CPU usage: {health.CpuUsagePercent}%");
            recommendations.Add("Consider scaling resources or reducing workload");
        }
        else if (health.CpuUsagePercent >= 70)
        {
            issues.Add($"High CPU usage: {health.CpuUsagePercent}%");
            recommendations.Add("Monitor CPU usage trends");
        }

        // Check Memory
        if (health.MemoryUsagePercent >= 90)
        {
            issues.Add($"Critical memory usage: {health.MemoryUsagePercent}%");
            recommendations.Add("Increase memory allocation or restart agent");
        }
        else if (health.MemoryUsagePercent >= 70)
        {
            issues.Add($"High memory usage: {health.MemoryUsagePercent}%");
            recommendations.Add("Monitor memory usage trends");
        }

        // Check Disk
        if (health.DiskUsagePercent >= 95)
        {
            issues.Add($"Critical disk usage: {health.DiskUsagePercent}%");
            recommendations.Add("Clear old logs or increase storage quota");
        }
        else if (health.DiskUsagePercent >= 80)
        {
            issues.Add($"High disk usage: {health.DiskUsagePercent}%");
            recommendations.Add("Monitor disk usage and plan for cleanup");
        }

        // Check connection failures
        if (health.FailedConnectionAttempts > 10)
        {
            issues.Add($"High failed connection attempts: {health.FailedConnectionAttempts}");
            recommendations.Add("Investigate potential attacks or misconfigurations");
        }

        return new HealthSummary
        {
            Status = DetermineHealthStatus(health),
            Issues = issues,
            Recommendations = recommendations,
            CpuUsage = health.CpuUsagePercent,
            MemoryUsage = health.MemoryUsagePercent,
            DiskUsage = health.DiskUsagePercent,
            ActiveConnections = health.ActiveConnections,
            FailedConnections = health.FailedConnectionAttempts
        };
    }
}

/// <summary>
/// State object for health check failure processing.
/// </summary>
public class HealthCheckFailureState
{
    public int FailureCount { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public bool ShouldMarkAsError { get; set; }
}

/// <summary>
/// Health summary with issues and recommendations.
/// </summary>
public class HealthSummary
{
    public HoneypotHealthStatus Status { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public decimal CpuUsage { get; set; }
    public decimal MemoryUsage { get; set; }
    public decimal DiskUsage { get; set; }
    public int ActiveConnections { get; set; }
    public int FailedConnections { get; set; }

    public bool HasIssues => Issues.Count > 0;
    public bool IsHealthy => Status == HoneypotHealthStatus.Healthy;
}
