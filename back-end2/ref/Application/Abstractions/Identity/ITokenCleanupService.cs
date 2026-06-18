namespace Application.Abstractions.Identity;

/// <summary>
/// Service for cleaning up expired tokens.
/// Follows ISP - single responsibility for token cleanup operations.
/// </summary>
public interface ITokenCleanupService
{
    /// <summary>
    /// Cleans up expired refresh tokens from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of tokens cleaned up.</returns>
    Task<int> CleanupExpiredRefreshTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired user sessions from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of sessions cleaned up.</returns>
    Task<int> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs full token cleanup (refresh tokens + sessions).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total number of records cleaned up.</returns>
    Task<int> PerformFullCleanupAsync(CancellationToken cancellationToken = default);
}
