namespace Trap_Intel.Domain.Honeypots
{
    /// <summary>
    /// Honeypot type enumeration.
    /// Represents different types of honeypot services that can be deployed.
    /// </summary>
    public enum HoneypotType
    {
        SSH = 0,
        HTTP = 1,
        FTP = 2,
        SMTP = 3,
        DNS = 4,
        Telnet = 5,
        RDP = 6,
        Samba = 7,
        SNMP = 8,
        Custom = 9
    }

    /// <summary>
    /// Honeypot status enumeration.
    /// Represents the current state of a honeypot deployment.
    /// </summary>
    public enum HoneypotStatus
    {
        Provisioning = 0,   // Being deployed to external service
        Active = 1,         // Running and capturing events
        Paused = 2,         // Deployed but not capturing
        Inactive = 3,       // Not running
        Error = 4,          // Connection or health check issues
        Terminated = 5,     // End of life / deleted
        Retired = 6         // Archived
    }

    /// <summary>
    /// Honeypot deployment location.
    /// Represents where the honeypot is deployed.
    /// </summary>
    public enum HoneypotDeploymentLocation
    {
        Cloud = 0,
        OnPremise = 1,
        Hybrid = 2
    }

    /// <summary>
    /// Honeypot health status.
    /// Represents the health state of a running honeypot.
    /// </summary>
    public enum HoneypotHealthStatus
    {
        Healthy = 0,
        Degraded = 1,
        Unhealthy = 2,
        Unknown = 3
    }

    /// <summary>
    /// Log capture level.
    /// Determines verbosity of captured events.
    /// </summary>
    public enum LogCaptureLevel
    {
        Minimal = 0,   // Only successful attacks
        Standard = 1,  // Standard logging
        Verbose = 2,   // All attempts including failed ones
        Debug = 3      // Full debugging information
    }

    /// <summary>
    /// Heartbeat status for monitoring Go honeypot connectivity.
    /// Tracks the health of communication between .NET platform and Go agents.
    /// </summary>
    public enum HeartbeatStatus
    {
        Healthy = 0,        // Receiving regular heartbeats
        Warning = 1,        // Missed 1-2 heartbeats
        Critical = 2,       // Missed 3+ heartbeats
        Offline = 3,        // Confirmed offline (no heartbeat for 5+ minutes)
        Unknown = 4         // Never received heartbeat
    }
}
