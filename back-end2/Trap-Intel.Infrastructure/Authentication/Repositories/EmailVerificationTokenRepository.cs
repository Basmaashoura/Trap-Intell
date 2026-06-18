using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Entities;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Authentication.Repositories;

/// <summary>
/// Repository implementation for email verification tokens.
/// </summary>
public sealed class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly ApplicationDbContext _context;

    public EmailVerificationTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        await _context.Set<EmailVerificationToken>().AddAsync(token, cancellationToken);
    }

    public async Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EmailVerificationToken>()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailVerificationToken>> GetActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<EmailVerificationToken>()
            .Where(t => t.UserId == userId
                && !t.IsRevoked
                && !t.IsUsed
                && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await _context.Set<EmailVerificationToken>()
            .Where(t => t.UserId == userId && !t.IsRevoked && !t.IsUsed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.IsRevoked, true)
                .SetProperty(t => t.RevokedAt, now),
                cancellationToken);
    }

    public async Task<int> DeleteExpiredTokensAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EmailVerificationToken>()
            .Where(t => t.ExpiresAt < olderThan)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public void Update(EmailVerificationToken token)
    {
        _context.Set<EmailVerificationToken>().Update(token);
    }
}
