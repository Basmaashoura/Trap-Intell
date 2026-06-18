using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Represents an active user session.
/// Used for session management and force logout during offboarding.
/// </summary>
public sealed class UserSession : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The user this session belongs to.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Unique token identifying this session (hashed).
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// Refresh token for this session (hashed).
    /// </summary>
    public string? RefreshTokenHash { get; private set; }

    /// <summary>
    /// When the session was created (login time).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the session expires.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// When the session was last active.
    /// </summary>
    public DateTime LastActivityAt { get; private set; }

    /// <summary>
    /// IP address of the client.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent string (browser/device info).
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Device identifier for mobile apps.
    /// </summary>
    public string? DeviceId { get; private set; }

    /// <summary>
    /// Whether this session is still valid.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// When the session was revoked.
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Reason for revocation.
    /// </summary>
    public string? RevocationReason { get; private set; }

    /// <summary>
    /// Navigation property to User.
    /// </summary>
    public User? User { get; init; }

    private UserSession()
    {
    }

    public static UserSession Create(
        Guid userId,
        string tokenHash,
        string? refreshTokenHash,
        DateTime expiresAt,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceId = null)
    {
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            RefreshTokenHash = refreshTokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            LastActivityAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceId = deviceId,
            IsActive = true
        };

        return session;
    }

    /// <summary>
    /// Updates the last activity timestamp.
    /// </summary>
    public void RecordActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Refreshes the session with a new token.
    /// </summary>
    public void Refresh(string newTokenHash, string? newRefreshTokenHash, DateTime newExpiresAt)
    {
        TokenHash = newTokenHash;
        RefreshTokenHash = newRefreshTokenHash;
        ExpiresAt = newExpiresAt;
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Revokes this session (logout or force logout).
    /// </summary>
    public void Revoke(string reason = "User logout")
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;

        Raise(new UserSessionRevokedDomainEvent(UserId, Id, reason));
    }

    /// <summary>
    /// Checks if the session is valid.
    /// </summary>
    public bool IsValid => IsActive && DateTime.UtcNow < ExpiresAt;

    /// <summary>
    /// Checks if the session has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}
