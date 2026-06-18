using System.Security.Cryptography;
using System.Text;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Identity.Entities;

/// <summary>
/// Represents a refresh token for JWT token rotation.
/// Implements secure token management with family-based reuse detection.
/// </summary>
public sealed class RefreshToken : Entity<Guid>
{
    /// <summary>
    /// The user this token belongs to.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// SHA-256 hash of the actual token. Never store raw tokens!
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// Token family ID for rotation chain tracking.
    /// All rotated tokens share the same family ID.
    /// If a token is reused after rotation, the entire family is revoked.
    /// </summary>
    public Guid FamilyId { get; private set; }

    /// <summary>
    /// When this token expires (absolute expiration).
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// When this token was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When this token was used (null if never used).
    /// </summary>
    public DateTime? UsedAt { get; private set; }

    /// <summary>
    /// Whether this token has been revoked.
    /// </summary>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// When this token was revoked (null if not revoked).
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Reason for revocation (logout, security, rotation, etc.).
    /// </summary>
    public string? RevocationReason { get; private set; }

    /// <summary>
    /// Whether this token has been used to generate a new token.
    /// </summary>
    public bool IsUsed { get; private set; }

    /// <summary>
    /// Reference to the token that replaced this one (for audit trail).
    /// </summary>
    public Guid? ReplacedByTokenId { get; private set; }

    /// <summary>
    /// Device information for security tracking.
    /// </summary>
    public string? DeviceInfo { get; private set; }

    /// <summary>
    /// IP address where token was created.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent string for device identification.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Navigation property to User.
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Checks if the token is currently valid (not expired, not revoked, not used).
    /// </summary>
    public bool IsValid => !IsRevoked && !IsUsed && !IsExpired;

    /// <summary>
    /// Checks if the token has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Time until token expires.
    /// </summary>
    public TimeSpan? TimeUntilExpiry => IsExpired ? null : ExpiresAt - DateTime.UtcNow;

    // Private constructor for EF Core
    private RefreshToken() { }

    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    /// <param name="userId">The user ID this token belongs to</param>
    /// <param name="token">The raw token (will be hashed)</param>
    /// <param name="expiresAt">Expiration time</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <param name="deviceInfo">Optional device information</param>
    /// <returns>A new RefreshToken entity</returns>
    public static RefreshToken Create(
        Guid userId,
        string token,
        DateTime expiresAt,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceInfo = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty", nameof(token));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration must be in the future", nameof(expiresAt));

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(token),
            FamilyId = Guid.NewGuid(), // New family for initial token
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false,
            IsUsed = false,
            IpAddress = SanitizeIpAddress(ipAddress),
            UserAgent = TruncateUserAgent(userAgent),
            DeviceInfo = deviceInfo?.Length > 500 ? deviceInfo[..500] : deviceInfo
        };

        refreshToken.RaiseDomainEvent(new RefreshTokenCreatedEvent(
            refreshToken.Id,
            userId,
            refreshToken.FamilyId,
            ipAddress,
            userAgent));

        return refreshToken;
    }

    /// <summary>
    /// Creates a rotated refresh token (replacement for a used token).
    /// The new token inherits the family ID from the original.
    /// </summary>
    /// <param name="originalToken">The original token being rotated</param>
    /// <param name="newToken">The new raw token</param>
    /// <param name="expiresAt">Expiration time for the new token</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <returns>A new RefreshToken entity in the same family</returns>
    public static RefreshToken CreateRotated(
        RefreshToken originalToken,
        string newToken,
        DateTime expiresAt,
        string? ipAddress = null,
        string? userAgent = null)
    {
        ArgumentNullException.ThrowIfNull(originalToken);

        if (string.IsNullOrWhiteSpace(newToken))
            throw new ArgumentException("Token cannot be empty", nameof(newToken));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration must be in the future", nameof(expiresAt));

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = originalToken.UserId,
            TokenHash = HashToken(newToken),
            FamilyId = originalToken.FamilyId, // Inherit family ID
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false,
            IsUsed = false,
            IpAddress = SanitizeIpAddress(ipAddress) ?? originalToken.IpAddress,
            UserAgent = TruncateUserAgent(userAgent) ?? originalToken.UserAgent,
            DeviceInfo = originalToken.DeviceInfo
        };

        refreshToken.RaiseDomainEvent(new RefreshTokenRotatedEvent(
            refreshToken.Id,
            originalToken.Id,
            originalToken.UserId,
            refreshToken.FamilyId,
            ipAddress));

        return refreshToken;
    }

    /// <summary>
    /// Marks this token as used and links to the replacement token.
    /// </summary>
    /// <param name="replacementTokenId">ID of the new token that replaced this one</param>
    public void MarkAsUsed(Guid replacementTokenId)
    {
        if (IsUsed)
            throw new InvalidOperationException("Token has already been used");

        if (IsRevoked)
            throw new InvalidOperationException("Cannot use a revoked token");

        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        ReplacedByTokenId = replacementTokenId;

        RaiseDomainEvent(new RefreshTokenUsedEvent(Id, UserId, FamilyId, replacementTokenId));
    }

    /// <summary>
    /// Revokes this token with a reason.
    /// </summary>
    /// <param name="reason">Reason for revocation</param>
    public void Revoke(string reason)
    {
        if (IsRevoked)
            return; // Already revoked, no-op

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;

        RaiseDomainEvent(new RefreshTokenRevokedEvent(Id, UserId, FamilyId, reason));
    }

    /// <summary>
    /// Revokes this token due to detected reuse (security threat).
    /// </summary>
    public void RevokeForReuse()
    {
        Revoke("Token reuse detected - potential security breach");
    }

    /// <summary>
    /// Verifies if a raw token matches this token's hash.
    /// </summary>
    /// <param name="token">The raw token to verify</param>
    /// <returns>True if the token matches</returns>
    public bool VerifyToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var hash = HashToken(token);
        return TokenHash.Equals(hash, StringComparison.Ordinal);
    }

    /// <summary>
    /// Generates a cryptographically secure random token.
    /// </summary>
    /// <returns>A base64-encoded random token (64 bytes / 512 bits)</returns>
    public static string GenerateSecureToken()
    {
        var randomBytes = new byte[64]; // 512 bits
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Hashes a token using SHA-256.
    /// </summary>
    /// <param name="token">The raw token to hash</param>
    /// <returns>The SHA-256 hash as a hex string</returns>
    public static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Sanitizes IP address to prevent injection attacks and ensure valid format.
    /// </summary>
    private static string? SanitizeIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return null;

        // Remove any non-valid characters (only allow digits, dots, colons for IPv4/IPv6)
        var sanitized = new string(ipAddress.Where(c => char.IsDigit(c) || c == '.' || c == ':').ToArray());
        
        // Truncate to max IPv6 length
        return sanitized.Length > 45 ? sanitized[..45] : sanitized;
    }

    /// <summary>
    /// Truncates user agent string to prevent database overflow.
    /// </summary>
    private static string? TruncateUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return null;

        return userAgent.Length > 1000 ? userAgent[..1000] : userAgent;
    }
}
