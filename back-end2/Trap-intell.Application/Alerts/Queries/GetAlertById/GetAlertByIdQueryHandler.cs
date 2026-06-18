using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Application.Abstractions.Alerts;

namespace Trap_Intel.Application.Alerts.Queries.GetAlertById;

internal sealed class GetAlertByIdQueryHandler : IRequestHandler<GetAlertByIdQuery, Result<AlertDetailDto>>
{
    private readonly IAlertQueryService _alertQueryService;

    public GetAlertByIdQueryHandler(IAlertQueryService alertQueryService)
    {
        _alertQueryService = alertQueryService;
    }

    public async Task<Result<AlertDetailDto>> Handle(GetAlertByIdQuery request, CancellationToken cancellationToken)
    {
        var alert = await _alertQueryService.GetAlertByIdAsync(request.OrganizationId, request.AlertId, cancellationToken);

        if (alert is null)
            return Result.Failure<AlertDetailDto>(AlertErrors.NotFound);

        return Result.Success(alert);
    }
}
