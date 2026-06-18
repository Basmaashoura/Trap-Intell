using Application.Abstractions.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity;

/// <summary>
/// Service for cleaning up expired tokens and sessions.
/// Implements SRP - only handles token cleanup operations.
/// </summary>
internal sealed class TokenCleanupService : ITokenCleanupService
{
    private readonly IdentityDbContext _identityDbContext;
    private readonly ILogger<TokenCleanupService> _logger;

    public TokenCleanupService(
        IdentityDbContext identityDbContext,
        ILogger<TokenCleanupService> logger)
    {
        _identityDbContext = identityDbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredRefreshTokensAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;

        // Find users with expired refresh tokens and clear them
        // Using raw SQL for efficiency with large datasets
        int affectedRows = await _identityDbContext.Users
            .Where(u => u.RefreshTokenExpiresAt.HasValue && u.RefreshTokenExpiresAt.Value < now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.RefreshTokenId, (string?)null)
                .SetProperty(u => u.RefreshTokenHash, (string?)null)
                .SetProperty(u => u.RefreshTokenExpiresAt, (DateTime?)null)
                .SetProperty(u => u.RefreshTokenCreatedAt, (DateTime?)null)
                .SetProperty(u => u.UpdatedAt, now),
                cancellationToken);

        if (affectedRows > 0)
        {
            _logger.LogInformation(
                "Cleaned up {Count} expired refresh tokens",
                affectedRows);
        }

        return affectedRows;
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        // Sessions are managed in the Domain layer via ApplicationDbContext
        // This is a placeholder for when sessions have expiration
        // Currently, sessions are revoked explicitly, not expired

        _logger.LogDebug("Session cleanup check completed - sessions are managed via explicit revocation");

        return 0;
    }

    /// <inheritdoc />
    public async Task<int> PerformFullCleanupAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting full token cleanup");

        int refreshTokensCleanedUp = await CleanupExpiredRefreshTokensAsync(cancellationToken);
        int sessionsCleanedUp = await CleanupExpiredSessionsAsync(cancellationToken);

        int totalCleanedUp = refreshTokensCleanedUp + sessionsCleanedUp;

        _logger.LogInformation(
            "Full token cleanup completed. Refresh tokens: {RefreshTokens}, Sessions: {Sessions}, Total: {Total}",
            refreshTokensCleanedUp,
            sessionsCleanedUp,
            totalCleanedUp);

        return totalCleanedUp;
    }
}
