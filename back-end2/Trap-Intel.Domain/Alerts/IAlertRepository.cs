using Trap_Intel.Domain.Alerts.Enums;

namespace Trap_Intel.Domain.Alerts;

public interface IAlertRepository
{
    Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Alert>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<List<Alert>> GetByStatusAsync(Guid organizationId, AlertStatus status, CancellationToken cancellationToken = default);
    Task<List<Alert>> GetBySeverityAsync(Guid organizationId, AlertSeverity minSeverity, CancellationToken cancellationToken = default);
    Task<List<Alert>> GetByTypeAsync(Guid organizationId, AlertType type, CancellationToken cancellationToken = default);
    Task<List<Alert>> GetUnacknowledgedAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<List<Alert>> GetAssignedToUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Alert>> GetRecentAsync(Guid organizationId, int hours = 24, CancellationToken cancellationToken = default);
    Task<List<Alert>> GetSnoozedExpiredAsync(CancellationToken cancellationToken = default);
    Task<List<Alert>> GetPendingEscalationAsync(CancellationToken cancellationToken = default);
    Task<List<Alert>> GetExpiredAlertsAsync(CancellationToken cancellationToken = default);
    Task<List<Alert>> GetBySourceIdAsync(Guid sourceId, CancellationToken cancellationToken = default);
    Task AddAsync(Alert alert, CancellationToken cancellationToken = default);
    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);
    Task<int> CountByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<int> CountUnacknowledgedAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<int> CountBySeverityAsync(Guid organizationId, AlertSeverity severity, CancellationToken cancellationToken = default);
}
