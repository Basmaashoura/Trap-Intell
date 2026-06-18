using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Application.Auditing.Queries.ExportAuditLogs;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.AuditLogs;

internal sealed class ExportAuditLogsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/auditlogs/export")
            .WithTags("Audit Logs")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/", HandleAsync)
            .WithName("ExportAuditLogs")
            .WithSummary("Exports audit logs to CSV based on dynamic filters")
            .WithDescription("Can export up to 10k logs based on advanced filtering capabilities for compliance and forensics.")
            .RequireAnalystOrAbove()
            .RequirePermission(Permissions.Reports.Export)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        Guid organizationId,
        [FromQuery] Guid? userId = null,
        [FromQuery] AuditAction? action = null,
        [FromQuery] AuditResourceType? resourceType = null,
        [FromQuery] AuditSeverity? severity = null,
        [FromQuery] string? ipAddress = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] ComplianceStandard? standard = null,
        [FromQuery] bool includeArchived = false,
        ISender? sender = null, 
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default)
    {
        if (sender is null || httpContext is null)
            return Results.StatusCode(StatusCodes.Status500InternalServerError);

        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
            return Results.Forbid();

        var query = new ExportAuditLogsQuery(
            organizationId, 
            userId, 
            action, 
            resourceType, 
            severity, 
            ipAddress, 
            startDate, 
            endDate, 
            standard, 
            includeArchived);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to export audit logs",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        var fileName = $"AuditLogs_{organizationId}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return Results.File(result.Value, "text/csv", fileName);
    }
}
