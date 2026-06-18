using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

/// <summary>
/// ASP.NET Identity user that links to Domain User.
/// Contains only authentication concerns - no business logic.
/// </summary>
public sealed class ApplicationIdentityUser : IdentityUser<Guid>
{
    /// <summary>
    /// Links to Domain User.Id for synchronization.
    /// </summary>
    public Guid DomainUserId { get; set; }

    /// <summary>
    /// When the identity user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the identity user was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Whether this identity is active.
    /// Used for soft-disable during offboarding.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Non-secret identifier for the refresh token, used for O(1) database lookups.
    /// This is a prefix of the token or a separate identifier that can be indexed.
    /// SECURITY: This is NOT secret - it's only used to find the user record quickly.
    /// </summary>
    public string? RefreshTokenId { get; set; }

    /// <summary>
    /// Hashed refresh token for JWT authentication.
    /// SECURITY: Never store plain text tokens!
    /// </summary>
    public string? RefreshTokenHash { get; set; }

    /// <summary>
    /// When the refresh token expires.
    /// </summary>
    public DateTime? RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// When the refresh token was created.
    /// Used for token rotation detection.
    /// </summary>
    public DateTime? RefreshTokenCreatedAt { get; set; }

    private ApplicationIdentityUser()
    {
    }

    public static ApplicationIdentityUser Create(Guid domainUserId, string email, string? userName = null)
    {
        return new ApplicationIdentityUser
        {
            Id = Guid.NewGuid(),
            DomainUserId = domainUserId,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = userName ?? email,
            NormalizedUserName = (userName ?? email).ToUpperInvariant(),
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = true,
            AccessFailedCount = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Sets a new refresh token (hashed for security).
    /// </summary>
    /// <param name="token">The plain text refresh token.</param>
    /// <param name="expiresAt">When the token expires.</param>
    public void SetRefreshToken(string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Refresh token cannot be empty", nameof(token));
        }

        // Store a non-secret identifier for O(1) database lookups
        // We use first 16 characters of the token as the lookup key
        RefreshTokenId = token.Length >= 16 ? token[..16] : token;
        RefreshTokenHash = HashToken(token);
        RefreshTokenExpiresAt = expiresAt;
        RefreshTokenCreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Revokes the current refresh token.
    /// </summary>
    public void RevokeRefreshToken()
    {
        RefreshTokenId = null;
        RefreshTokenHash = null;
        RefreshTokenExpiresAt = null;
        RefreshTokenCreatedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates a refresh token against the stored hash.
    /// SECURITY: Uses constant-time comparison to prevent timing attacks.
    /// </summary>
    /// <param name="token">The plain text token to validate.</param>
    /// <returns>True if the token is valid and not expired.</returns>
    public bool IsRefreshTokenValid(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        // Check expiration first
        if (!RefreshTokenExpiresAt.HasValue || RefreshTokenExpiresAt.Value <= DateTime.UtcNow)
        {
            return false;
        }

        // Validate hash (constant-time comparison)
        if (string.IsNullOrEmpty(RefreshTokenHash))
        {
            return false;
        }

        string tokenHash = HashToken(token);
        return ConstantTimeEquals(RefreshTokenHash, tokenHash);
    }

    /// <summary>
    /// Deactivates the user account.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        LockoutEnd = DateTimeOffset.MaxValue;
        RevokeRefreshToken();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the user account.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Extracts the token identifier (first 16 characters) from a refresh token.
    /// Used for O(1) database lookups.
    /// </summary>
    /// <param name="token">The plain text refresh token.</param>
    /// <returns>The token identifier.</returns>
    public static string GetTokenId(string token)
    {
        return token.Length >= 16 ? token[..16] : token;
    }

    /// <summary>
    /// Hashes a token using SHA256.
    /// SECURITY: One-way hash - cannot be reversed.
    /// </summary>
    /// <param name="token">The plain text token.</param>
    /// <returns>Base64-encoded hash.</returns>
    private static string HashToken(string token)
    {
        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
        byte[] hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks.
    /// SECURITY: Uses CryptographicOperations.FixedTimeEquals for true constant-time comparison.
    /// </summary>
    private static bool ConstantTimeEquals(string a, string b)
    {
        byte[] aBytes = Encoding.UTF8.GetBytes(a);
        byte[] bBytes = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
