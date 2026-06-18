using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Contracts;
using Trap_Intel.Application.Auditing.Queries.GetAuditLogs;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Application.Auditing.Queries.GetCriticalAuditLogs;
using Trap_Intel.Application.Auditing.Queries.GetAuditLogChanges;
using Trap_Intel.Application.Auditing.Queries.GetUnacknowledgedCriticalAuditCount;
using Trap_Intel.Application.Auditing.Queries.GetAuditLogsSummary;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.AuditLogs;

internal sealed class ForensicAuditEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/auditlogs")
            .WithTags("Audit Logs")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/critical", GetCriticalLogs)
            .WithName("GetCriticalAuditLogs")
            .WithSummary("Retrieves all critical and warning tier audit logs for a specified organization")
            .RequireAnalystOrAbove()
            .RequirePermission(Permissions.Reports.View)
            .Produces<PagedResult<AuditTrailDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/critical/unacknowledged-count", GetUnacknowledgedCriticalCount)
            .WithName("GetUnacknowledgedCriticalAuditCount")
            .WithSummary("Returns the count of unacknowledged critical audit logs")
            .RequireAnalystOrAbove()
            .RequirePermission(Permissions.Reports.View)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/summary", GetSummary)
            .WithName("GetAuditLogsSummary")
            .WithSummary("Returns aggregated summary metrics for audit logs")
            .WithDescription("Provides totals and grouped distributions by severity, action, and resource type with optional date range and archival filtering")
            .RequireAnalystOrAbove()
            .RequirePermission(Permissions.Reports.View)
            .Produces<AuditLogsSummaryDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/{auditTrailId:guid}/changes", GetLogChanges)
            .WithName("GetAuditLogChanges")
            .WithSummary("Perform forensic checking: returns old vs new property changes for a specific log")
            .RequireAnalystOrAbove()
            .RequirePermission(Permissions.Reports.View)
            .Produces<IEnumerable<AuditChangeDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetCriticalLogs(
        Guid organizationId,
        [AsParameters] GlobalListQueryRequest listQuery,
        ISender? sender = null, 
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default)
    {
        if (sender is null || httpContext is null)
            return Results.StatusCode(StatusCodes.Status500InternalServerError);

        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
            return Results.Forbid();

        var query = new GetCriticalAuditLogsQuery(organizationId, listQuery.ToQueryOptions());
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(title: "Request Failed", detail: result.Errors.FirstOrDefault()?.Message);
        }

        var filterKey = listQuery.BuildFilterKey(("severitytier", "critical_warning"));
        httpContext.Response.SetListRealtimeHeaders("auditlogs", "organization", filterKey);

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetLogChanges(
        Guid organizationId,
        Guid auditTrailId,
        ISender? sender = null, 
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default)
    {
        if (sender is null || httpContext is null)
            return Results.StatusCode(StatusCodes.Status500InternalServerError);

        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
            return Results.Forbid();

        var query = new GetAuditLogChangesQuery(organizationId, auditTrailId);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            return error?.Code == "Auditing.InvalidResourceId" 
                ? Results.NotFound(new { message = error.Message })
                : Results.Problem(title: "Request Failed", detail: error?.Message);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetUnacknowledgedCriticalCount(
        Guid organizationId,
        ISender? sender = null,
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default)
    {
        if (sender is null || httpContext is null)
            return Results.StatusCode(StatusCodes.Status500InternalServerError);

        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
            return Results.Forbid();

        var query = new GetUnacknowledgedCriticalAuditCountQuery(organizationId);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(title: "Request Failed", detail: result.Errors.FirstOrDefault()?.Message);
        }

        return Results.Ok(new { count = result.Value });
    }

    private static async Task<IResult> GetSummary(
        Guid organizationId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool includeArchived = true,
        [FromQuery] int top = 5,
        ISender? sender = null,
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default)
    {
        if (sender is null || httpContext is null)
            return Results.StatusCode(StatusCodes.Status500InternalServerError);

        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
            return Results.Forbid();

        var query = new GetAuditLogsSummaryQuery(
            OrganizationId: organizationId,
            StartDate: startDate,
            EndDate: endDate,
            IncludeArchived: includeArchived,
            Top: top);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Request Failed",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result.Value);
    }
}
