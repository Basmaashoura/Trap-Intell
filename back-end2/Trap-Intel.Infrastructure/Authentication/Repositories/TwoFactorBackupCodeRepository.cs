using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Entities;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Authentication.Repositories;

/// <summary>
/// Repository implementation for two-factor authentication backup codes.
/// </summary>
public sealed class TwoFactorBackupCodeRepository : ITwoFactorBackupCodeRepository
{
    private readonly ApplicationDbContext _context;

    public TwoFactorBackupCodeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TwoFactorBackupCode code, CancellationToken cancellationToken = default)
    {
        await _context.Set<TwoFactorBackupCode>().AddAsync(code, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<TwoFactorBackupCode> codes, CancellationToken cancellationToken = default)
    {
        await _context.Set<TwoFactorBackupCode>().AddRangeAsync(codes, cancellationToken);
    }

    public async Task<IReadOnlyList<TwoFactorBackupCode>> GetUnusedCodesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<TwoFactorBackupCode>()
            .Where(c => c.UserId == userId && !c.IsUsed)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnusedCodeCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TwoFactorBackupCode>()
            .CountAsync(c => c.UserId == userId && !c.IsUsed, cancellationToken);
    }

    public async Task<TwoFactorBackupCode?> FindByCodeHashAsync(
        Guid userId,
        string codeHash,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<TwoFactorBackupCode>()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CodeHash == codeHash && !c.IsUsed, cancellationToken);
    }

    public void Update(TwoFactorBackupCode code)
    {
        _context.Set<TwoFactorBackupCode>().Update(code);
    }

    public async Task DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _context.Set<TwoFactorBackupCode>()
            .Where(c => c.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> DeleteUsedCodesOlderThanAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TwoFactorBackupCode>()
            .Where(c => c.IsUsed && c.UsedAt < olderThan)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
