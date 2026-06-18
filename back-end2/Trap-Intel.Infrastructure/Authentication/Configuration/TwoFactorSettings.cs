using System.ComponentModel.DataAnnotations;

namespace Trap_Intel.Infrastructure.Authentication.Configuration;

/// <summary>
/// Configuration settings for Two-Factor Authentication.
/// </summary>
public sealed class TwoFactorSettings
{
    public const string SectionName = "TwoFactorSettings";

    /// <summary>
    /// Application name shown in authenticator apps.
    /// </summary>
    [Required]
    public string Issuer { get; set; } = "Trap-Intel";

    /// <summary>
    /// Number of backup codes to generate.
    /// </summary>
    [Range(5, 20)]
    public int BackupCodeCount { get; set; } = 10;

    /// <summary>
    /// TOTP time window tolerance (number of periods before/after).
    /// Default is 1, allowing codes from 30 seconds before and after current time.
    /// </summary>
    [Range(0, 3)]
    public int TotpTimeTolerance { get; set; } = 1;

    /// <summary>
    /// TOTP time step in seconds.
    /// Standard is 30 seconds.
    /// </summary>
    [Range(30, 60)]
    public int TotpTimeStepSeconds { get; set; } = 30;

    /// <summary>
    /// Number of digits in TOTP code.
    /// Standard is 6.
    /// </summary>
    [Range(6, 8)]
    public int TotpDigits { get; set; } = 6;

    /// <summary>
    /// Whether to require 2FA verification for sensitive operations
    /// even if the user session is already authenticated.
    /// </summary>
    public bool RequireRecentVerificationForSensitiveOps { get; set; } = true;

    /// <summary>
    /// How long (in minutes) a 2FA verification is considered "recent".
    /// Used for step-up authentication for sensitive operations.
    /// </summary>
    [Range(5, 60)]
    public int RecentVerificationWindowMinutes { get; set; } = 15;

    /// <summary>
    /// Maximum number of 2FA verification attempts before temporary lockout.
    /// </summary>
    [Range(3, 10)]
    public int MaxVerificationAttempts { get; set; } = 5;

    /// <summary>
    /// Lockout duration in minutes after max verification attempts exceeded.
    /// </summary>
    [Range(5, 60)]
    public int VerificationLockoutMinutes { get; set; } = 15;

    /// <summary>
    /// Whether to send email notification when 2FA is enabled or disabled.
    /// </summary>
    public bool SendNotificationOnStatusChange { get; set; } = true;

    /// <summary>
    /// Whether to alert user when backup codes are running low.
    /// </summary>
    public bool AlertOnLowBackupCodes { get; set; } = true;

    /// <summary>
    /// Threshold for low backup codes alert.
    /// </summary>
    [Range(1, 5)]
    public int LowBackupCodesThreshold { get; set; } = 3;
}
