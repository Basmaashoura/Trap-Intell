using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Entities;
using Trap_Intel.Infrastructure.Authentication.Configuration;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Authentication.Repositories;

/// <summary>
/// EF Core implementation of IRefreshTokenRepository.
/// </summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RefreshTokenRepository> _logger;
    private readonly RefreshTokenSettings _settings;

    public RefreshTokenRepository(
        ApplicationDbContext context,
        ILogger<RefreshTokenRepository> logger,
        IOptions<RefreshTokenSettings> settings)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        // Don't use AsNoTracking here - token will be modified (marked as used/revoked)
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetByIdAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == tokenId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.RefreshTokens
            .Where(t => t.UserId == userId 
                && !t.IsRevoked 
                && !t.IsUsed 
                && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RefreshToken>> GetByFamilyAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Where(t => t.FamilyId == familyId)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(token, cancellationToken);
        // Note: SaveChanges is called here for simplicity. 
        // In a full UoW pattern, this would be deferred to the UoW.
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        // If entity is not tracked, attach it
        var entry = _context.Entry(token);
        if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
        {
            _context.RefreshTokens.Attach(token);
            entry.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        }
        
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> RevokeAllForUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var tokensToRevoke = await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in tokensToRevoke)
        {
            token.Revoke(reason);
        }

        var count = await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "Revoked {Count} tokens for user {UserId}. Reason: {Reason}",
            tokensToRevoke.Count, userId, reason);

        return tokensToRevoke.Count;
    }

    /// <inheritdoc />
    public async Task<int> RevokeAllInFamilyAsync(Guid familyId, string reason, CancellationToken cancellationToken = default)
    {
        var tokensToRevoke = await _context.RefreshTokens
            .Where(t => t.FamilyId == familyId && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in tokensToRevoke)
        {
            token.Revoke(reason);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Revoked {Count} tokens in family {FamilyId}. Reason: {Reason}",
            tokensToRevoke.Count, familyId, reason);

        return tokensToRevoke.Count;
    }

    /// <inheritdoc />
    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        // Delete tokens that expired more than the configured retention period
        var cutoffDate = DateTime.UtcNow.AddDays(-_settings.ExpiredTokenRetentionDays);
        
        // Use ExecuteDeleteAsync for better performance (EF Core 7+)
        var deletedCount = await _context.RefreshTokens
            .Where(t => t.ExpiresAt < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount > 0)
        {
            _logger.LogInformation(
                "Deleted {Count} expired refresh tokens older than {CutoffDate}",
                deletedCount, cutoffDate);
        }

        return deletedCount;
    }

    /// <inheritdoc />
    public async Task<int> CountActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.RefreshTokens
            .CountAsync(t => t.UserId == userId 
                && !t.IsRevoked 
                && !t.IsUsed 
                && t.ExpiresAt > now, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasExceededMaxSessionsAsync(Guid userId, int maxSessions, CancellationToken cancellationToken = default)
    {
        var activeCount = await CountActiveSessionsAsync(userId, cancellationToken);
        return activeCount >= maxSessions;
    }
}
