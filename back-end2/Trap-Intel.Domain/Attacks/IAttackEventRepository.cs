namespace Trap_Intel.Domain.Attacks;

public interface IAttackEventRepository
{
    Task<AttackEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AttackEvent?> GetByExternalIdAsync(string externalEventId, CancellationToken cancellationToken = default);
    Task<List<AttackEvent>> GetByHoneypotIdAsync(Guid honeypotId, CancellationToken cancellationToken = default);
    Task<List<AttackEvent>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<List<AttackEvent>> GetBySourceIPAsync(string sourceIP, CancellationToken cancellationToken = default);
    Task<List<AttackEvent>> GetUnanalyzedAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task<List<AttackEvent>> GetHighSeverityAsync(DateTime since, CancellationToken cancellationToken = default);
    Task AddAsync(AttackEvent attackEvent, CancellationToken cancellationToken = default);
    Task UpdateAsync(AttackEvent attackEvent, CancellationToken cancellationToken = default);
    Task<int> CountByHoneypotAsync(Guid honeypotId, CancellationToken cancellationToken = default);
}
