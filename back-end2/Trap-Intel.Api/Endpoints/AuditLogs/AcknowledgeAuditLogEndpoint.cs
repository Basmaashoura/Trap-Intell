using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Application.Auditing.Commands.AcknowledgeAuditLog;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.AuditLogs;

internal sealed class AcknowledgeAuditLogEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/auditlogs/{id:guid}/acknowledge")
            .WithTags("Audit Logs")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapPost("/", HandleAsync)
            .WithName("AcknowledgeAuditLog")
            .WithSummary("Acknowledges a critical audit log")
            .WithDescription("Allows an administrator to review and acknowledge a critical system event")
            .RequireAnalystOrAbove()
            .RequirePermission(Permissions.Alerts.Acknowledge)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid organizationId,
        Guid id,
        [FromBody] AcknowledgeAuditLogRequest request,
        ISender? sender = null, 
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default)
    {
        if (sender is null || httpContext is null)
            return Results.StatusCode(StatusCodes.Status500InternalServerError);

        // Extract user ID from token
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        // Security Check: Token MUST have an organization ID that matches or be a Super Admin
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

        var command = new AcknowledgeAuditLogCommand(organizationId, id, userId, request.Notes);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            // Specifically handling Not Found vs other business errors
            if (result.Errors.Any(e => e.Code == "Auditing.AuditTrailNotFound"))
            {
                return Results.NotFound(new { Error = "Audit log not found." });
            }

            if (result.Errors.Any(e => e.Code == "Auditing.InvalidResourceId"))
            {
                return Results.NotFound(new { Error = "Audit log not found for this organization." });
            }

            if (result.Errors.Any(e => e.Code == "Auditing.AlreadyAcknowledged"))
            {
                // Keep acknowledge operation idempotent for clients and test sweeps.
                return Results.NoContent();
            }

            return Results.Problem(
                title: "Failed to acknowledge audit log",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.NoContent();
    }
}

public record AcknowledgeAuditLogRequest(string? Notes);
