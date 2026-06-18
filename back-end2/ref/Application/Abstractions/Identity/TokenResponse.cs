namespace Application.Abstractions.Identity;

/// <summary>
/// Response containing JWT tokens.
/// </summary>
public sealed class TokenResponse
{
    /// <summary>
    /// The JWT access token.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// The refresh token for obtaining new access tokens.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// When the access token expires (UTC).
    /// </summary>
    public required DateTime AccessTokenExpiresAt { get; init; }

    /// <summary>
    /// When the refresh token expires (UTC).
    /// </summary>
    public required DateTime RefreshTokenExpiresAt { get; init; }

    /// <summary>
    /// Token type (always "Bearer").
    /// </summary>
    public string TokenType => "Bearer";

    /// <summary>
    /// Access token lifetime in seconds.
    /// </summary>
    public int ExpiresIn => (int)(AccessTokenExpiresAt - DateTime.UtcNow).TotalSeconds;
}
