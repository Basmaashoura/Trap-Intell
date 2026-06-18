using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Application.Auditing.Queries.VerifyAuditLogIntegrity;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.AuditLogs;

internal sealed class VerifyAuditLogIntegrityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/auditlogs/verify")
            .WithTags("Audit Logs")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/", HandleAsync)
            .WithName("VerifyAuditLogIntegrity")
            .WithSummary("Scans the audit trail to confirm data validity and catch tampering")
            .WithDescription("Checks the embedded SHA-256 signatures of properties for manipulation.")
            .RequireAnalystOrAbove()
            .RequirePermission(Permissions.Reports.View)
            .Produces<AuditIntegrityResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        Guid organizationId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        ISender? sender = null, 
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default)
    {
        if (sender is null || httpContext is null)
            return Results.StatusCode(StatusCodes.Status500InternalServerError);

        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
            return Results.Forbid();

        var query = new VerifyAuditLogIntegrityQuery(organizationId, startDate, endDate);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to verify audit logs",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result.Value);
    }
}
