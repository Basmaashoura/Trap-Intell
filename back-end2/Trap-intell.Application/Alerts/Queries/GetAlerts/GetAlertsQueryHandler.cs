using MediatR;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Application.Abstractions.Alerts;

namespace Trap_Intel.Application.Alerts.Queries.GetAlerts;

internal sealed class GetAlertsQueryHandler : IRequestHandler<GetAlertsQuery, Result<PagedResult<AlertDto>>>
{
    private readonly IAlertQueryService _alertQueryService;

    public GetAlertsQueryHandler(IAlertQueryService alertQueryService)
    {
        _alertQueryService = alertQueryService;
    }

    public async Task<Result<PagedResult<AlertDto>>> Handle(GetAlertsQuery request, CancellationToken cancellationToken)
    {
        var queryOptions = request.Query ?? new GlobalQueryOptions();

        var alerts = await _alertQueryService.GetAlertsAsync(
            request.OrganizationId,
            request.Status,
            request.Severity,
            request.Type,
            request.AssignedUserId,
            queryOptions,
            cancellationToken);

        return Result.Success(alerts);
    }
}
