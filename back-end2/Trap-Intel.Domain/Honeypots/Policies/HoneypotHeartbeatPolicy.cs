using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Honeypots.Policies;

/// <summary>
/// Policy object for honeypot heartbeat monitoring.
/// Encapsulates all heartbeat-related business logic.
/// </summary>
public class HoneypotHeartbeatPolicy
{
    private const int OFFLINE_THRESHOLD_MINUTES = 5;
    private const int HEARTBEAT_TIMEOUT_SECONDS = 90;
    private const int MAX_MISSED_HEARTBEATS_BEFORE_OFFLINE = 3;

    /// <summary>
    /// Validate and process heartbeat from Go agent.
    /// </summary>
    public static Result<HeartbeatUpdateState> ProcessHeartbeat(
        Honeypot honeypot,
        string agentId,
        string agentVersion)
    {
        // Validation
        if (honeypot.Status != HoneypotStatus.Active && honeypot.Status != HoneypotStatus.Paused)
            return Result.Failure<HeartbeatUpdateState>(HoneypotErrors.NotDeployed);

        if (string.IsNullOrWhiteSpace(agentId))
            return Result.Failure<HeartbeatUpdateState>(HoneypotErrors.InvalidAgentId);

        // Calculate recovery state
        bool wasOffline = !honeypot.IsConnected;
        TimeSpan? downtime = CalculateDowntime(honeypot, wasOffline);

        // Build update state
        var updateState = new HeartbeatUpdateState
        {
            AgentId = agentId,
            AgentVersion = agentVersion,
            NewStatus = HeartbeatStatus.Healthy,
            WasOffline = wasOffline,
            Downtime = downtime,
            MissedHeartbeatsReset = true
        };

        return Result.Success(updateState);
    }

    /// <summary>
    /// Process missed heartbeat.
    /// </summary>
    public static Result<MissedHeartbeatState> ProcessMissedHeartbeat(Honeypot honeypot)
    {
        // Skip terminated honeypots
        if (IsTerminated(honeypot))
            return Result.Success(MissedHeartbeatState.Ignored());

        var oldStatus = honeypot.HeartbeatStatus;
        var newMissedCount = honeypot.ConsecutiveMissedHeartbeats + 1;
        var newStatus = DetermineHeartbeatStatus(newMissedCount);
        bool wentOffline = newMissedCount >= MAX_MISSED_HEARTBEATS_BEFORE_OFFLINE;

        var state = new MissedHeartbeatState
        {
            OldStatus = oldStatus,
            NewStatus = newStatus,
            MissedCount = newMissedCount,
            WentOffline = wentOffline
        };

        return Result.Success(state);
    }

    /// <summary>
    /// Check if honeypot is offline.
    /// </summary>
    public static bool IsOffline(Honeypot honeypot)
    {
        if (!honeypot.LastHeartbeatAt.HasValue)
            return honeypot.Status == HoneypotStatus.Active || 
                   honeypot.Status == HoneypotStatus.Paused;

        var timeSince = DateTime.UtcNow - honeypot.LastHeartbeatAt.Value;
        return timeSince > TimeSpan.FromMinutes(OFFLINE_THRESHOLD_MINUTES);
    }

    /// <summary>
    /// Check if heartbeat is overdue.
    /// </summary>
    public static bool IsHeartbeatOverdue(Honeypot honeypot)
    {
        if (!honeypot.LastHeartbeatAt.HasValue)
            return honeypot.Status == HoneypotStatus.Active || 
                   honeypot.Status == HoneypotStatus.Paused;

        var timeSince = DateTime.UtcNow - honeypot.LastHeartbeatAt.Value;
        return timeSince > TimeSpan.FromSeconds(HEARTBEAT_TIMEOUT_SECONDS);
    }

    /// <summary>
    /// Get time since last heartbeat.
    /// </summary>
    public static TimeSpan? GetTimeSinceLastHeartbeat(Honeypot honeypot)
    {
        if (!honeypot.LastHeartbeatAt.HasValue)
            return null;

        return DateTime.UtcNow - honeypot.LastHeartbeatAt.Value;
    }

    #region Private Helpers

    private static TimeSpan? CalculateDowntime(Honeypot honeypot, bool wasOffline)
    {
        if (!wasOffline)
            return null;

        // Honeypot has a private field _lastOfflineAt that we can't access
        // So we estimate based on last heartbeat
        if (honeypot.LastHeartbeatAt.HasValue)
        {
            return DateTime.UtcNow - honeypot.LastHeartbeatAt.Value;
        }

        return null;
    }

    private static HeartbeatStatus DetermineHeartbeatStatus(int missedCount)
    {
        return missedCount switch
        {
            >= 3 => HeartbeatStatus.Offline,
            2 => HeartbeatStatus.Warning,
            1 => HeartbeatStatus.Critical,
            _ => HeartbeatStatus.Healthy
        };
    }

    private static bool IsTerminated(Honeypot honeypot) =>
        honeypot.Status == HoneypotStatus.Terminated ||
        honeypot.Status == HoneypotStatus.Retired;

    #endregion
}

/// <summary>
/// State object for heartbeat updates.
/// </summary>
public class HeartbeatUpdateState
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public HeartbeatStatus NewStatus { get; set; }
    public bool WasOffline { get; set; }
    public TimeSpan? Downtime { get; set; }
    public bool MissedHeartbeatsReset { get; set; }
}

/// <summary>
/// State object for missed heartbeat processing.
/// </summary>
public class MissedHeartbeatState
{
    public HeartbeatStatus OldStatus { get; set; }
    public HeartbeatStatus NewStatus { get; set; }
    public int MissedCount { get; set; }
    public bool WentOffline { get; set; }
    public bool ShouldIgnore { get; set; }

    public static MissedHeartbeatState Ignored() => new() { ShouldIgnore = true };
}
