using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Identity.Entities;

/// <summary>
/// Represents an email verification token.
/// Used to verify user email addresses during registration or email changes.
/// </summary>
public sealed class EmailVerificationToken : SecureTokenBase
{
    private const int DefaultExpirationHours = 24;

    /// <summary>
    /// Navigation property to User.
    /// </summary>
    public User? User { get; private set; }

    // Private constructor for EF Core
    private EmailVerificationToken() { }

    /// <summary>
    /// Creates a new email verification token.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="expirationHours">Hours until token expires. Default is 24 hours.</param>
    /// <returns>Tuple of (EmailVerificationToken entity, raw token string for sending to user).</returns>
    /// <exception cref="ArgumentException">Thrown when userId is empty.</exception>
    public static (EmailVerificationToken Token, string RawToken) Create(
        Guid userId,
        int? expirationHours = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);

        var token = new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(expirationHours ?? DefaultExpirationHours),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
            IsRevoked = false
        };

        return (token, rawToken);
    }

    /// <summary>
    /// Reconstructs an email verification token from persistence.
    /// </summary>
    public static EmailVerificationToken Reconstruct(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime createdAt,
        bool isUsed,
        DateTime? usedAt,
        bool isRevoked,
        DateTime? revokedAt)
    {
        return new EmailVerificationToken
        {
            Id = id,
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
            IsUsed = isUsed,
            UsedAt = usedAt,
            IsRevoked = isRevoked,
            RevokedAt = revokedAt
        };
    }

    /// <summary>
    /// Marks the token as used.
    /// </summary>
    /// <returns>Result indicating success or failure.</returns>
    public Result Use()
    {
        if (IsUsed)
            return Result.Failure(IdentityErrors.EmailVerificationTokenAlreadyUsed);

        if (IsRevoked)
            return Result.Failure(IdentityErrors.EmailVerificationTokenRevoked);

        if (IsExpired)
            return Result.Failure(IdentityErrors.EmailVerificationTokenExpired);

        IsUsed = true;
        UsedAt = DateTime.UtcNow;

        return Result.Success();
    }
}
