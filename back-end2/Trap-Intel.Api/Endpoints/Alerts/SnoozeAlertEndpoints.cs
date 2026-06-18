using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trap_Intel.Application.Alerts.Commands.SnoozeAlert;
using Trap_Intel.Application.Alerts.Commands.UnsnoozeAlert;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;

namespace Trap_Intel.Api.Endpoints.Alerts;

internal sealed class SnoozeAlertEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/alerts")
            .WithTags("Alerts")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapPut("/{alertId:guid}/snooze", SnoozeAlert)
            .WithName("SnoozeAlert")
            .WithSummary("Snoozes an alert for a specified duration")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{alertId:guid}/unsnooze", UnsnoozeAlert)
            .WithName("UnsnoozeAlert")
            .WithSummary("Wakes up / Removes the snooze from an alert")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> SnoozeAlert(
        Guid organizationId,
        Guid alertId,
        [FromQuery] int minutes,
        [FromQuery] string? reason,
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Enforce Organization boundary
        var orgClaim = httpContext.User.GetOrganizationClaimValue();
        if (orgClaim != null && Guid.TryParse(orgClaim, out var claimOrgId) && claimOrgId != organizationId)
            return Results.Forbid();

        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Results.Unauthorized();

        if (minutes <= 0)
        {
            return Results.Problem(title: "Invalid Request", detail: "Snooze duration must be greater than zero.", statusCode: StatusCodes.Status400BadRequest);
        }

        var duration = TimeSpan.FromMinutes(minutes);
        var command = new SnoozeAlertCommand(organizationId, alertId, userId, duration, reason);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            return error?.Code == "Alert.NotFound" 
                ? Results.NotFound(new { message = error.Message })
                : Results.Problem(title: "Snooze Failed", detail: error?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = $"Alert snoozed for {minutes} minutes." });
    }

    private static async Task<IResult> UnsnoozeAlert(
        Guid organizationId,
        Guid alertId,
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Enforce Organization boundary
        var orgClaim = httpContext.User.GetOrganizationClaimValue();
        if (orgClaim != null && Guid.TryParse(orgClaim, out var claimOrgId) && claimOrgId != organizationId)
            return Results.Forbid();

        var command = new UnsnoozeAlertCommand(organizationId, alertId);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            return error?.Code == "Alert.NotFound" 
                ? Results.NotFound(new { message = error.Message }) 
                : Results.Problem(title: "Unsnooze Failed", detail: error?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Alert is active again." });
    }
}
