using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Entities;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Authentication.Repositories;

/// <summary>
/// Repository implementation for password reset tokens.
/// </summary>
public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly ApplicationDbContext _context;

    public PasswordResetTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        await _context.Set<PasswordResetToken>().AddAsync(token, cancellationToken);
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PasswordResetToken>()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<PasswordResetToken>> GetActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<PasswordResetToken>()
            .Where(t => t.UserId == userId
                && !t.IsRevoked
                && !t.IsUsed
                && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetRecentTokenCountAsync(Guid userId, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        return await _context.Set<PasswordResetToken>()
            .CountAsync(t => t.UserId == userId && t.CreatedAt >= cutoff, cancellationToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await _context.Set<PasswordResetToken>()
            .Where(t => t.UserId == userId && !t.IsRevoked && !t.IsUsed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.IsRevoked, true)
                .SetProperty(t => t.RevokedAt, now),
                cancellationToken);
    }

    public async Task<int> DeleteExpiredTokensAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PasswordResetToken>()
            .Where(t => t.ExpiresAt < olderThan)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public void Update(PasswordResetToken token)
    {
        _context.Set<PasswordResetToken>().Update(token);
    }
}
