using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Application.Alerts.Queries.GetAlerts;
using Trap_Intel.Application.Alerts.Queries.GetAlertById;
using Trap_Intel.Application.Alerts.Queries.GetAlertDashboardStatistics;
using Trap_Intel.Domain.Alerts.Enums;

namespace Trap_Intel.Application.Abstractions.Alerts;

public interface IAlertQueryService
{
    Task<PagedResult<AlertDto>> GetAlertsAsync(Guid organizationId, AlertStatus? status, AlertSeverity? severity, AlertType? type, Guid? assignedUserId, GlobalQueryOptions queryOptions, CancellationToken cancellationToken = default);
    Task<AlertDetailDto?> GetAlertByIdAsync(Guid organizationId, Guid alertId, CancellationToken cancellationToken = default);
    Task<AlertDashboardStatisticsDto> GetDashboardStatisticsAsync(Guid organizationId, int lastNDays, CancellationToken cancellationToken = default);
}
