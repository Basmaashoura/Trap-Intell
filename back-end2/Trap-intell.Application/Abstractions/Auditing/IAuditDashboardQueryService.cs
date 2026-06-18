using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Application.Auditing.Queries.GetAuditDashboardStatistics;

namespace Trap_Intel.Application.Abstractions.Auditing;

public interface IAuditDashboardQueryService
{
    Task<AuditDashboardStatisticsDto> GetDashboardStatisticsAsync(Guid organizationId, int lastNDays, CancellationToken cancellationToken = default);
}
