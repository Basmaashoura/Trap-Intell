using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Application.Abstractions.Alerts;

namespace Trap_Intel.Application.Alerts.Queries.GetAlertDashboardStatistics;

internal sealed class GetAlertDashboardStatisticsQueryHandler : IRequestHandler<GetAlertDashboardStatisticsQuery, Result<AlertDashboardStatisticsDto>>
{
    private readonly IAlertQueryService _alertQueryService;

    public GetAlertDashboardStatisticsQueryHandler(IAlertQueryService alertQueryService)
    {
        _alertQueryService = alertQueryService;
    }

    public async Task<Result<AlertDashboardStatisticsDto>> Handle(GetAlertDashboardStatisticsQuery request, CancellationToken cancellationToken)
    {
        var statistics = await _alertQueryService.GetDashboardStatisticsAsync(request.OrganizationId, request.LastNDays, cancellationToken);
        return Result.Success(statistics);
    }
}
