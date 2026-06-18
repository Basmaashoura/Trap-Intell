using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trap_Intel.Application.Alerts.Commands.AcknowledgeAlert;
using Trap_Intel.Application.Alerts.Commands.ResolveAlert;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;

namespace Trap_Intel.Api.Endpoints.Alerts;

internal sealed class AlertActionEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/alerts")
            .WithTags("Alerts")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapPut("/{alertId:guid}/acknowledge", AcknowledgeAlert)
            .WithName("AcknowledgeAlert")
            .WithSummary("Acknowledges a security alert")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{alertId:guid}/resolve", ResolveAlert)
            .WithName("ResolveAlert")
            .WithSummary("Marks a security alert as resolved or false positive")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> AcknowledgeAlert(
        Guid organizationId,
        Guid alertId,
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var orgClaim = httpContext.User.GetOrganizationClaimValue();
        if (orgClaim != null && Guid.TryParse(orgClaim, out var claimOrgId) && claimOrgId != organizationId)
            return Results.Forbid();

        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Results.Unauthorized();

        var result = await sender.Send(new AcknowledgeAlertCommand(organizationId, alertId, userId), cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            return error?.Code == "Alert.NotFound" 
                ? Results.NotFound(new { message = error.Message })
                : Results.Problem(title: "Failed to acknowledge", detail: error?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Alert acknowledged." });
    }

    private static async Task<IResult> ResolveAlert(
        Guid organizationId,
        Guid alertId,
        [FromBody] AlertResolutionRequest request,
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var orgClaim = httpContext.User.GetOrganizationClaimValue();
        if (orgClaim != null && Guid.TryParse(orgClaim, out var claimOrgId) && claimOrgId != organizationId)
            return Results.Forbid();

        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Results.Unauthorized();

        var result = await sender.Send(new ResolveAlertCommand(
            organizationId, 
            alertId, 
            userId, 
            request.Resolution, 
            request.IsFalsePositive), cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            return error?.Code == "Alert.NotFound" 
                ? Results.NotFound(new { message = error.Message })
                : Results.Problem(title: "Failed to resolve", detail: error?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Alert resolved successfully." });
    }
}

public sealed record AlertResolutionRequest(
    string Resolution,
    bool IsFalsePositive = false
);
