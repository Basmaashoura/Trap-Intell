using System.ComponentModel.DataAnnotations;

namespace Trap_Intel.Infrastructure.Authentication.Configuration;

/// <summary>
/// Email service settings for SMTP configuration.
/// </summary>
public sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";

    /// <summary>
    /// SMTP server host.
    /// </summary>
    [Required]
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port.
    /// </summary>
    [Range(1, 65535)]
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// SMTP username for authentication.
    /// </summary>
    public string? SmtpUsername { get; set; }

    /// <summary>
    /// SMTP password for authentication.
    /// </summary>
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// Whether to use SSL/TLS.
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Sender email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>
    /// Sender display name.
    /// </summary>
    [Required]
    public string SenderName { get; set; } = "Trap-Intel";

    /// <summary>
    /// Base URL for the frontend application (used for generating links).
    /// </summary>
    [Required]
    public string FrontendBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Email verification link path (appended to FrontendBaseUrl).
    /// </summary>
    public string EmailVerificationPath { get; set; } = "/verify-email";

    /// <summary>
    /// Password reset link path (appended to FrontendBaseUrl).
    /// </summary>
    public string PasswordResetPath { get; set; } = "/reset-password";

    /// <summary>
    /// Organization invitation acceptance path (appended to FrontendBaseUrl).
    /// </summary>
    public string OrganizationInvitationPath { get; set; } = "/invitations/accept";
}

/// <summary>
/// Settings for email verification tokens.
/// </summary>
public sealed class EmailVerificationSettings
{
    public const string SectionName = "EmailVerificationSettings";

    /// <summary>
    /// Hours until email verification token expires.
    /// </summary>
    [Range(1, 168)] // 1 hour to 7 days
    public int TokenExpirationHours { get; set; } = 24;

    /// <summary>
    /// Whether email verification is required before login.
    /// </summary>
    public bool RequireEmailVerification { get; set; } = true;
}

/// <summary>
/// Settings for password reset tokens.
/// </summary>
public sealed class PasswordResetSettings
{
    public const string SectionName = "PasswordResetSettings";

    /// <summary>
    /// Minutes until password reset token expires.
    /// </summary>
    [Range(5, 1440)] // 5 minutes to 24 hours
    public int TokenExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum password reset requests per time window.
    /// </summary>
    [Range(1, 20)]
    public int MaxRequestsPerWindow { get; set; } = 3;

    /// <summary>
    /// Time window in minutes for rate limiting.
    /// </summary>
    [Range(1, 1440)]
    public int RateLimitWindowMinutes { get; set; } = 60;
}

/// <summary>
/// Settings for token cleanup background service.
/// </summary>
public sealed class TokenCleanupSettings
{
    public const string SectionName = "TokenCleanupSettings";

    /// <summary>
    /// Interval in hours between cleanup runs.
    /// </summary>
    [Range(1, 168)] // 1 hour to 7 days
    public int CleanupIntervalHours { get; set; } = 6;

    /// <summary>
    /// Number of days to retain expired tokens for audit purposes.
    /// </summary>
    [Range(1, 90)]
    public int RetentionDays { get; set; } = 7;
}
