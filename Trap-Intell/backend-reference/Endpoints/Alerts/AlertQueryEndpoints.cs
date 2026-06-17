using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Contracts;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Application.Alerts.Queries.GetAlertById;
using Trap_Intel.Application.Alerts.Queries.GetAlertDashboardStatistics;
using Trap_Intel.Application.Alerts.Queries.GetAlerts;
using Trap_Intel.Domain.Alerts.Enums;

namespace Trap_Intel.Api.Endpoints.Alerts;

internal sealed class AlertQueryEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/alerts")
            .WithTags("Alerts")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/", GetAlertsAsync)
            .WithName("GetAlerts")
            .WithSummary("Search and filter alerts for an organization")
            .Produces<PagedResult<AlertDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", GetAlertByIdAsync)
            .WithName("GetAlertById")
            .WithSummary("Get alert details including actions and comments")
            .Produces<AlertDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/dashboard", GetAlertDashboardStatisticsAsync)
            .WithName("GetAlertDashboardStatistics")
            .WithSummary("Get overview metrics for SOC dashboard")
            .Produces<AlertDashboardStatisticsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetAlertsAsync(
        Guid organizationId,
        [AsParameters] GlobalListQueryRequest listQuery,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        [FromQuery] AlertStatus? status = null,
        [FromQuery] AlertSeverity? severity = null,
        [FromQuery] AlertType? type = null,
        [FromQuery] Guid? assignedUserId = null)
    {
        // Implementation retrieves and filters alerts based on criteria
    }

    private static async Task<IResult> GetAlertByIdAsync(
        Guid organizationId,
        Guid id,
        ISender? sender = null,
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default)
    {
        // Implementation retrieves detailed alert information
    }

    private static async Task<IResult> GetAlertDashboardStatisticsAsync(
        Guid organizationId,
        [FromQuery] int lastNDays = 30,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation calculates dashboard statistics
    }
}
