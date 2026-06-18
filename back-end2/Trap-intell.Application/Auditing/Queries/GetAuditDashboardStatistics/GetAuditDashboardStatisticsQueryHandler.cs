using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Application.Abstractions.Auditing;

namespace Trap_Intel.Application.Auditing.Queries.GetAuditDashboardStatistics;

internal sealed class GetAuditDashboardStatisticsQueryHandler : IRequestHandler<GetAuditDashboardStatisticsQuery, Result<AuditDashboardStatisticsDto>>
{
    private readonly IAuditDashboardQueryService _dashboardQueryService;

    public GetAuditDashboardStatisticsQueryHandler(IAuditDashboardQueryService dashboardQueryService)
    {
        _dashboardQueryService = dashboardQueryService;
    }

    public async Task<Result<AuditDashboardStatisticsDto>> Handle(GetAuditDashboardStatisticsQuery request, CancellationToken cancellationToken)
    {
        var statistics = await _dashboardQueryService.GetDashboardStatisticsAsync(request.OrganizationId, request.LastNDays, cancellationToken);
        return Result.Success(statistics);
    }
}
