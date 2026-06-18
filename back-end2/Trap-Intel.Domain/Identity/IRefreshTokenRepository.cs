using Trap_Intel.Domain.Identity.Entities;

namespace Trap_Intel.Domain.Identity;

/// <summary>
/// Repository interface for RefreshToken entity operations.
/// Provides methods for token lifecycle management and security operations.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Gets a refresh token by its hash.
    /// </summary>
    /// <param name="tokenHash">The SHA-256 hash of the token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The refresh token if found, null otherwise</returns>
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a refresh token by its ID.
    /// </summary>
    /// <param name="tokenId">The token ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The refresh token if found, null otherwise</returns>
    Task<RefreshToken?> GetByIdAsync(Guid tokenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active (not revoked, not used, not expired) tokens for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active refresh tokens</returns>
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tokens in a token family (rotation chain).
    /// </summary>
    /// <param name="familyId">The family ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tokens in the family</returns>
    Task<IReadOnlyList<RefreshToken>> GetByFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new refresh token.
    /// </summary>
    /// <param name="token">The refresh token to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing refresh token.
    /// </summary>
    /// <param name="token">The refresh token to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all tokens for a user (e.g., on logout from all devices).
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="reason">Reason for revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens revoked</returns>
    Task<int> RevokeAllForUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all tokens in a family (used when reuse is detected).
    /// </summary>
    /// <param name="familyId">The family ID</param>
    /// <param name="reason">Reason for revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens revoked</returns>
    Task<int> RevokeAllInFamilyAsync(Guid familyId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all expired tokens (cleanup job).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens deleted</returns>
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts active sessions (valid tokens) for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of active sessions</returns>
    Task<int> CountActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has exceeded the maximum allowed sessions.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="maxSessions">Maximum allowed sessions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if limit exceeded</returns>
    Task<bool> HasExceededMaxSessionsAsync(Guid userId, int maxSessions, CancellationToken cancellationToken = default);
}
