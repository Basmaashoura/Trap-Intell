using Trap_Intel.Domain.ThreatActors.Enums;

namespace Trap_Intel.Domain.ThreatActors;

public interface IThreatActorRepository
{
    Task<ThreatActor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ThreatActor?> GetByIPAddressAsync(Guid organizationId, string ipAddress, CancellationToken cancellationToken = default);
    Task<List<ThreatActor>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<List<ThreatActor>> GetByThreatLevelAsync(Guid organizationId, ThreatLevel minLevel, CancellationToken cancellationToken = default);
    Task<List<ThreatActor>> GetActiveThreatsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<List<ThreatActor>> GetByStatusAsync(Guid organizationId, ThreatActorStatus status, CancellationToken cancellationToken = default);
    Task<List<ThreatActor>> GetByTypeAsync(Guid organizationId, ThreatActorType type, CancellationToken cancellationToken = default);
    Task<List<ThreatActor>> GetRecentAsync(Guid organizationId, int days = 7, CancellationToken cancellationToken = default);
    Task<List<ThreatActor>> GetTopThreatsByScoreAsync(Guid organizationId, int count = 10, CancellationToken cancellationToken = default);
    Task<ThreatActor?> FindByAttackEventIdAsync(Guid attackEventId, CancellationToken cancellationToken = default);
    Task AddAsync(ThreatActor threatActor, CancellationToken cancellationToken = default);
    Task UpdateAsync(ThreatActor threatActor, CancellationToken cancellationToken = default);
    Task<int> CountByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<int> CountActiveAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithIPAsync(Guid organizationId, string ipAddress, CancellationToken cancellationToken = default);
}
