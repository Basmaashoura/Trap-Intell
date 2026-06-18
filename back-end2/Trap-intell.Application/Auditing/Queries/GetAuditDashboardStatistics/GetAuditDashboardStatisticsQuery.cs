using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Auditing.Queries.GetAuditDashboardStatistics;

public record GetAuditDashboardStatisticsQuery(
    Guid OrganizationId,
    int LastNDays = 30) : IRequest<Result<AuditDashboardStatisticsDto>>;
