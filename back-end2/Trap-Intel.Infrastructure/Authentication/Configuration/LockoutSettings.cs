namespace Trap_Intel.Infrastructure.Authentication.Configuration;

/// <summary>
/// Account lockout configuration settings.
/// </summary>
public sealed class LockoutSettings
{
    public const string SectionName = "Authentication:Lockout";

    /// <summary>
    /// Maximum failed login attempts before lockout.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Lockout duration in minutes.
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Whether to enable lockout for new users.
    /// </summary>
    public bool EnableLockout { get; set; } = true;

    /// <summary>
    /// Whether to lockout SuperAdmin accounts.
    /// </summary>
    public bool LockoutSuperAdmin { get; set; } = false;

    /// <summary>
    /// Progressive lockout multiplier (each subsequent lockout is longer).
    /// </summary>
    public double ProgressiveLockoutMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Maximum lockout duration in minutes.
    /// </summary>
    public int MaxLockoutDurationMinutes { get; set; } = 1440; // 24 hours
}
