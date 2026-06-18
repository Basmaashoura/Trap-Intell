using Trap_Intel.Domain.Identity.Entities;

namespace Trap_Intel.Domain.Identity;

/// <summary>
/// Repository interface for password reset token operations.
/// </summary>
public interface IPasswordResetTokenRepository
{
    /// <summary>
    /// Adds a new password reset token.
    /// </summary>
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a token by its hash.
    /// </summary>
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active (non-used, non-revoked, non-expired) tokens for a user.
    /// </summary>
    Task<IReadOnlyList<PasswordResetToken>> GetActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of tokens created for a user within a time window.
    /// Used for rate limiting.
    /// </summary>
    Task<int> GetRecentTokenCountAsync(Guid userId, TimeSpan timeWindow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active tokens for a user.
    /// Used when generating a new token or when password is reset.
    /// </summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired tokens older than the specified date.
    /// </summary>
    Task<int> DeleteExpiredTokensAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing token.
    /// </summary>
    void Update(PasswordResetToken token);
}
