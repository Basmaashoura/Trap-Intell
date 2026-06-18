using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Contracts;
using Trap_Intel.Application.Auditing.Queries.GetAuditLogs;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.AuditLogs;

internal sealed class GetAuditLogsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/auditlogs")
            .WithTags("Audit Logs")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization(); // Requires Org Admin or specific "ViewAuditLogs" permission

        group.MapGet("/", HandleAsync)
            .WithName("GetAuditLogs")
            .WithSummary("Retrieves all audit logs for a specified organization")
            .WithDescription("Supports advanced filtering by action, resource type, severity, user, IP, date range, reason text, compliance standard, archival/acknowledgment flags, and configurable sorting")
            .RequireAnalystOrAbove()
            .RequirePermission(Permissions.Reports.View)
            .Produces<PagedResult<AuditTrailDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        Guid organizationId,
        [AsParameters] GlobalListQueryRequest listQuery,
        [FromQuery] AuditAction? action = null,
        [FromQuery] AuditResourceType? resourceType = null,
        [FromQuery] AuditSeverity? severity = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? ipAddress = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] ComplianceStandard? standard = null,
        [FromQuery] bool includeArchived = false,
        [FromQuery] bool? isAcknowledged = null,
        [FromQuery] string? reasonContains = null,
        [FromQuery] AuditTrailSortBy sortBy = AuditTrailSortBy.Timestamp,
        [FromQuery] AuditTrailSortDirection sortDirection = AuditTrailSortDirection.Desc,
        ISender? sender = null, 
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default)
    {
        if (sender is null || httpContext is null)
            return Results.StatusCode(StatusCodes.Status500InternalServerError);

        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
            return Results.Forbid();

        var query = new GetAuditLogsQuery(
            OrganizationId: organizationId,
            Query: listQuery.ToQueryOptions(),
            Action: action,
            ResourceType: resourceType,
            Severity: severity,
            UserId: userId,
            IpAddress: ipAddress,
            StartDate: startDate,
            EndDate: endDate,
            Standard: standard,
            IncludeArchived: includeArchived,
            IsAcknowledged: isAcknowledged,
            ReasonContains: reasonContains,
            SortBy: sortBy,
            SortDirection: sortDirection);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to retrieve audit logs",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        var filterKey = listQuery.BuildFilterKey(
            ("action", action),
            ("resourcetype", resourceType),
            ("severity", severity),
            ("userid", userId),
            ("ipaddress", ipAddress),
            ("standard", standard),
            ("includearchived", includeArchived),
            ("isacknowledged", isAcknowledged),
            ("reasoncontains", reasonContains),
            ("sortbyenum", sortBy),
            ("sortdirectionenum", sortDirection));

        httpContext.Response.SetListRealtimeHeaders("auditlogs", "organization", filterKey);

        return Results.Ok(result.Value);
    }
}
