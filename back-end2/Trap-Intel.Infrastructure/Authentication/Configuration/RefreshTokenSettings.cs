using System.ComponentModel.DataAnnotations;

namespace Trap_Intel.Infrastructure.Authentication.Configuration;

/// <summary>
/// Configuration settings for refresh token management.
/// </summary>
public class RefreshTokenSettings : IValidatableObject
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Authentication:RefreshToken";

    /// <summary>
    /// Token lifetime in days for normal login.
    /// Default: 7 days
    /// </summary>
    [Range(1, 365)]
    public int TokenLifetimeDays { get; set; } = 7;

    /// <summary>
    /// Token lifetime in days for "Remember Me" login.
    /// Default: 30 days
    /// </summary>
    [Range(7, 365)]
    public int RememberMeTokenLifetimeDays { get; set; } = 30;

    /// <summary>
    /// Maximum number of active sessions per user.
    /// Default: 5
    /// </summary>
    [Range(1, 20)]
    public int MaxActiveSessions { get; set; } = 5;

    /// <summary>
    /// Whether to revoke oldest session when max is exceeded.
    /// If false, new session creation will be rejected.
    /// Default: true
    /// </summary>
    public bool RevokeOldestOnMaxExceeded { get; set; } = true;

    /// <summary>
    /// Days after which expired tokens are deleted from database.
    /// Default: 30 days
    /// </summary>
    [Range(1, 365)]
    public int ExpiredTokenRetentionDays { get; set; } = 30;

    /// <summary>
    /// Whether to enable token reuse detection.
    /// When enabled, if a used token is presented again, all tokens in the family are revoked.
    /// Default: true (highly recommended for security)
    /// </summary>
    public bool EnableReuseDetection { get; set; } = true;

    /// <summary>
    /// Time window in seconds during which the same token can be used for concurrent requests.
    /// This prevents issues with concurrent API calls during token rotation.
    /// Default: 10 seconds
    /// </summary>
    [Range(0, 60)]
    public int ConcurrentUseGracePeriodSeconds { get; set; } = 10;

    /// <summary>
    /// Gets the token lifetime based on whether "Remember Me" is enabled.
    /// </summary>
    public TimeSpan GetTokenLifetime(bool rememberMe) =>
        TimeSpan.FromDays(rememberMe ? RememberMeTokenLifetimeDays : TokenLifetimeDays);

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RememberMeTokenLifetimeDays < TokenLifetimeDays)
        {
            yield return new ValidationResult(
                "RememberMeTokenLifetimeDays must be greater than or equal to TokenLifetimeDays",
                new[] { nameof(RememberMeTokenLifetimeDays), nameof(TokenLifetimeDays) });
        }
    }
}
