using Trap_Intel.Domain.Identity.Entities;

namespace Trap_Intel.Domain.Identity;

/// <summary>
/// Repository interface for email verification token operations.
/// </summary>
public interface IEmailVerificationTokenRepository
{
    /// <summary>
    /// Adds a new email verification token.
    /// </summary>
    Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a token by its hash.
    /// </summary>
    Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active (non-used, non-revoked, non-expired) tokens for a user.
    /// </summary>
    Task<IReadOnlyList<EmailVerificationToken>> GetActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active tokens for a user.
    /// Used when generating a new token or when user confirms email.
    /// </summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired tokens older than the specified date.
    /// </summary>
    Task<int> DeleteExpiredTokensAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing token.
    /// </summary>
    void Update(EmailVerificationToken token);
}
