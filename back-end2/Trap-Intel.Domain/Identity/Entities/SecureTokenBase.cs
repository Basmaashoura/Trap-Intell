using System.Security.Cryptography;
using System.Text;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Identity.Entities;

/// <summary>
/// Base class for secure verification tokens.
/// Provides common functionality for token hashing, validation, and lifecycle management.
/// </summary>
public abstract class SecureTokenBase : Entity<Guid>
{
    protected const int TokenSizeInBytes = 32; // 256-bit token

    /// <summary>
    /// The user this token belongs to.
    /// </summary>
    public Guid UserId { get; protected set; }

    /// <summary>
    /// SHA-256 hash of the actual token. Never store raw tokens!
    /// </summary>
    public string TokenHash { get; protected set; } = string.Empty;

    /// <summary>
    /// When this token expires.
    /// </summary>
    public DateTime ExpiresAt { get; protected set; }

    /// <summary>
    /// When this token was created.
    /// </summary>
    public DateTime CreatedAt { get; protected set; }

    /// <summary>
    /// Whether this token has been used.
    /// </summary>
    public bool IsUsed { get; protected set; }

    /// <summary>
    /// When this token was used (null if not used).
    /// </summary>
    public DateTime? UsedAt { get; protected set; }

    /// <summary>
    /// Whether this token has been revoked.
    /// </summary>
    public bool IsRevoked { get; protected set; }

    /// <summary>
    /// When this token was revoked (null if not revoked).
    /// </summary>
    public DateTime? RevokedAt { get; protected set; }

    /// <summary>
    /// Checks if the token is currently valid (not expired, not revoked, not used).
    /// </summary>
    public bool IsValid => !IsRevoked && !IsUsed && !IsExpired;

    /// <summary>
    /// Checks if the token has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Time remaining until expiration.
    /// </summary>
    public TimeSpan TimeUntilExpiry => IsExpired ? TimeSpan.Zero : ExpiresAt - DateTime.UtcNow;

    /// <summary>
    /// Revokes the token.
    /// </summary>
    public void Revoke()
    {
        if (!IsRevoked)
        {
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Validates a raw token against this token's hash using timing-safe comparison.
    /// </summary>
    /// <param name="rawToken">The raw token to validate.</param>
    /// <returns>True if the token matches.</returns>
    public bool ValidateToken(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return false;

        var hash = HashToken(rawToken);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(TokenHash),
            Encoding.UTF8.GetBytes(hash));
    }

    /// <summary>
    /// Generates a cryptographically secure random token.
    /// </summary>
    /// <returns>URL-safe Base64-encoded token string.</returns>
    protected static string GenerateSecureToken()
    {
        var tokenBytes = new byte[TokenSizeInBytes];
        RandomNumberGenerator.Fill(tokenBytes);
        return Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")  // URL-safe
            .Replace("/", "_")  // URL-safe
            .Replace("=", "");  // Remove padding
    }

    /// <summary>
    /// Hashes a token using SHA-256.
    /// </summary>
    /// <param name="token">The raw token to hash.</param>
    /// <returns>Lowercase hexadecimal string of the hash.</returns>
    public static string HashToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Sanitizes and truncates a string for safe storage.
    /// </summary>
    protected static string? SanitizeString(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Remove control characters and trim
        var sanitized = new string(value.Where(c => !char.IsControl(c)).ToArray()).Trim();
        
        return sanitized.Length > maxLength
            ? sanitized[..maxLength]
            : sanitized;
    }
}
