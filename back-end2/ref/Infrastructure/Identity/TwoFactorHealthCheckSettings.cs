namespace Infrastructure.Identity;

/// <summary>
/// Settings for 2FA health check background service.
/// </summary>
public sealed class TwoFactorHealthCheckSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "TwoFactorHealthCheck";

    /// <summary>
    /// Whether the health check is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval between health checks in minutes.
    /// </summary>
    public int IntervalMinutes { get; set; } = 60;
}
