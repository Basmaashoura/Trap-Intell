using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Identity.Entities;

/// <summary>
/// Represents a password reset token.
/// Used to allow users to reset their password via email.
/// </summary>
public sealed class PasswordResetToken : SecureTokenBase
{
    private const int DefaultExpirationMinutes = 60; // 1 hour by default

    /// <summary>
    /// IP address where the token was requested (for security auditing).
    /// </summary>
    public string? RequestedFromIp { get; private set; }

    /// <summary>
    /// User agent where the token was requested (for security auditing).
    /// </summary>
    public string? RequestedFromUserAgent { get; private set; }

    /// <summary>
    /// IP address where the token was used (for security auditing).
    /// </summary>
    public string? UsedFromIp { get; private set; }

    /// <summary>
    /// Navigation property to User.
    /// </summary>
    public User? User { get; private set; }

    // Private constructor for EF Core
    private PasswordResetToken() { }

    /// <summary>
    /// Creates a new password reset token.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="requestedFromIp">IP address of the requester.</param>
    /// <param name="requestedFromUserAgent">User agent of the requester.</param>
    /// <param name="expirationMinutes">Minutes until token expires. Default is 60 minutes.</param>
    /// <returns>Tuple of (PasswordResetToken entity, raw token string for sending to user).</returns>
    /// <exception cref="ArgumentException">Thrown when userId is empty.</exception>
    public static (PasswordResetToken Token, string RawToken) Create(
        Guid userId,
        string? requestedFromIp = null,
        string? requestedFromUserAgent = null,
        int? expirationMinutes = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);

        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes ?? DefaultExpirationMinutes),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
            IsRevoked = false,
            RequestedFromIp = SanitizeString(requestedFromIp, 45),
            RequestedFromUserAgent = SanitizeString(requestedFromUserAgent, 500)
        };

        return (token, rawToken);
    }

    /// <summary>
    /// Reconstructs a password reset token from persistence.
    /// </summary>
    public static PasswordResetToken Reconstruct(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime createdAt,
        bool isUsed,
        DateTime? usedAt,
        bool isRevoked,
        DateTime? revokedAt,
        string? requestedFromIp,
        string? requestedFromUserAgent,
        string? usedFromIp)
    {
        return new PasswordResetToken
        {
            Id = id,
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
            IsUsed = isUsed,
            UsedAt = usedAt,
            IsRevoked = isRevoked,
            RevokedAt = revokedAt,
            RequestedFromIp = requestedFromIp,
            RequestedFromUserAgent = requestedFromUserAgent,
            UsedFromIp = usedFromIp
        };
    }

    /// <summary>
    /// Marks the token as used.
    /// </summary>
    /// <param name="usedFromIp">IP address where the token was used.</param>
    /// <returns>Result indicating success or failure.</returns>
    public Result Use(string? usedFromIp = null)
    {
        if (IsUsed)
            return Result.Failure(IdentityErrors.PasswordResetTokenAlreadyUsed);

        if (IsRevoked)
            return Result.Failure(IdentityErrors.PasswordResetTokenRevoked);

        if (IsExpired)
            return Result.Failure(IdentityErrors.PasswordResetTokenExpired);

        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        UsedFromIp = SanitizeString(usedFromIp, 45);

        return Result.Success();
    }
}
