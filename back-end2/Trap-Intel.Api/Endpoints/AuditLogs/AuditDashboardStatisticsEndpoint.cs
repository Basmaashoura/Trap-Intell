using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Application.Auditing.Queries.GetAuditDashboardStatistics;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.AuditLogs;

internal sealed class AuditDashboardStatisticsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/auditlogs/dashboard")
            .WithTags("Audit Logs")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/", HandleAsync)
            .WithName("GetAuditDashboardStatistics")
            .WithSummary("Gets overview statistics for auditing dashboard")
            .WithDescription("Retrieves the total critical events, top resources, and latest unacknowledged critical events.")
            .RequireAnalystOrAbove()
            .RequirePermission(Permissions.Dashboards.View)
            .Produces<AuditDashboardStatisticsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        Guid organizationId,
        [FromQuery] int lastNDays = 30,
        ISender? sender = null, 
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default)
    {
        if (sender is null || httpContext is null)
            return Results.StatusCode(StatusCodes.Status500InternalServerError);

        // Security Check: Ensure Org Isolation
        var userOrgClaim = httpContext.User.GetOrganizationClaimValue();
        if (userOrgClaim != null && Guid.TryParse(userOrgClaim, out var claimOrgId))
        {
            if (claimOrgId != organizationId)
            {
                if (!httpContext.User.IsSuperAdmin())
                {
                    return Results.Forbid();
                }
            }
        }

        var query = new GetAuditDashboardStatisticsQuery(organizationId, lastNDays);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to retrieve audit dashboard statistics",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result.Value);
    }
}
