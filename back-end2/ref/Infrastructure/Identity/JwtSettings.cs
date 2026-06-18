namespace Infrastructure.Identity;

/// <summary>
/// JWT configuration settings.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Secret key for signing tokens.
    /// </summary>
    public string Secret { get; init; } = string.Empty;

    /// <summary>
    /// Token issuer.
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Token audience.
    /// </summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Access token expiration in minutes.
    /// Default: 20 minutes.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; init; } = 20;

    /// <summary>
    /// Refresh token expiration in days.
    /// Default: 7 days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; init; } = 7;
}
