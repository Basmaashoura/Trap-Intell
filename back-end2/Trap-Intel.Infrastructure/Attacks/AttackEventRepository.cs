using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Attacks;
using Trap_Intel.Domain.Attacks.Enums;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Attacks;

internal sealed class AttackEventRepository : IAttackEventRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AttackEventRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AttackEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AttackEvents
            .FirstOrDefaultAsync(attackEvent => attackEvent.Id == id, cancellationToken);
    }

    public async Task<AttackEvent?> GetByExternalIdAsync(string externalEventId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalEventId))
        {
            return null;
        }

        var normalizedExternalId = externalEventId.Trim();

        return await _dbContext.AttackEvents
            .FirstOrDefaultAsync(attackEvent => attackEvent.ExternalEventId == normalizedExternalId, cancellationToken);
    }

    public async Task<List<AttackEvent>> GetByHoneypotIdAsync(Guid honeypotId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AttackEvents
            .Where(attackEvent => attackEvent.HoneypotId == honeypotId)
            .OrderByDescending(attackEvent => attackEvent.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AttackEvent>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AttackEvents
            .Where(attackEvent => attackEvent.OrganizationId == organizationId)
            .OrderByDescending(attackEvent => attackEvent.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AttackEvent>> GetBySourceIPAsync(string sourceIP, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceIP))
        {
            return [];
        }

        var normalizedSourceIp = sourceIP.Trim();

        return await _dbContext.AttackEvents
            .Where(attackEvent => attackEvent.SourceEndpoint.IPAddress == normalizedSourceIp)
            .OrderByDescending(attackEvent => attackEvent.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AttackEvent>> GetUnanalyzedAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        var normalizedLimit = limit < 1 ? 1 : limit;

        return await _dbContext.AttackEvents
            .Where(attackEvent => !attackEvent.IsAnalyzed)
            .OrderBy(attackEvent => attackEvent.ReceivedAt)
            .Take(normalizedLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AttackEvent>> GetHighSeverityAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AttackEvents
            .Where(attackEvent =>
                attackEvent.Timestamp >= since &&
                (attackEvent.Severity == AttackSeverity.High || attackEvent.Severity == AttackSeverity.Critical))
            .OrderByDescending(attackEvent => attackEvent.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AttackEvent attackEvent, CancellationToken cancellationToken = default)
    {
        await _dbContext.AttackEvents.AddAsync(attackEvent, cancellationToken);
    }

    public Task UpdateAsync(AttackEvent attackEvent, CancellationToken cancellationToken = default)
    {
        _dbContext.AttackEvents.Update(attackEvent);
        return Task.CompletedTask;
    }

    public async Task<int> CountByHoneypotAsync(Guid honeypotId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AttackEvents
            .CountAsync(attackEvent => attackEvent.HoneypotId == honeypotId, cancellationToken);
    }
}
