using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Contracts;
using Trap_Intel.Application.Auditing.Queries.GetAuditLogs;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Api.Endpoints.AuditLogs;

internal sealed class GetAuditLogsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/auditlogs")
            .WithTags("Audit Logs")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/", HandleAsync)
            .WithName("GetAuditLogs")
            .WithSummary("Query audit logs with advanced filtering")
            .WithDescription("Retrieve organization audit trail with multiple filter options")
            .RequirePermission(Permissions.Reports.View)
            .Produces<PagedResult<AuditTrailDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
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
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation retrieves filtered audit logs with pagination
    }
}
