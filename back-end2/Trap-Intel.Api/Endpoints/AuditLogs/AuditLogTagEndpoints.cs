using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Application.Auditing.Commands.TagAuditLog;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.AuditLogs;

internal sealed class AuditLogTagEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/auditlogs")
            .WithTags("Audit Logs")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapPost("/{auditTrailId:guid}/tags", TagAuditLog)
            .WithName("TagAuditLog")
            .WithSummary("Tags an existing audit log with a compliance standard")
            .RequireAnalystOrAbove()
            .RequirePermission(Permissions.Alerts.Configure)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> TagAuditLog(
        Guid organizationId,
        Guid auditTrailId,
        [FromQuery] ComplianceStandard standard,
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Enforce the boundary
        var orgClaim = httpContext.User.GetOrganizationClaimValue();
        if (orgClaim != null && Guid.TryParse(orgClaim, out var claimOrgId) && claimOrgId != organizationId)
            return Results.Forbid();

        var command = new TagAuditLogCommand(organizationId, auditTrailId, standard);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            return error?.Code == "Auditing.InvalidResourceId" 
                ? Results.NotFound(new { message = error.Message })
                : Results.Problem(title: "Tagging Failed", detail: error?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = $"Log {auditTrailId} successfully tagged with {standard} compliance." });
    }
}
