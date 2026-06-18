using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trap_Intel.Application.Alerts.Commands.AssignAlert;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;

namespace Trap_Intel.Api.Endpoints.Alerts;

internal sealed class AssignAlertEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/alerts")
            .WithTags("Alerts")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapPut("/{alertId:guid}/assign", AssignAlert)
            .WithName("AssignAlert")
            .WithSummary("Assigns an alert to a specific security analyst/user")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> AssignAlert(
        Guid organizationId,
        Guid alertId,
        [FromQuery] Guid targetUserId,
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

        if (targetUserId == Guid.Empty)
            return Results.BadRequest(new { message = "Target user ID must be provided." });

        var command = new AssignAlertCommand(organizationId, alertId, targetUserId, userId);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            return error?.Code == "Alert.NotFound" 
                ? Results.NotFound(new { message = error.Message })
                : Results.Problem(title: "Assignment Failed", detail: error?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Alert assigned successfully." });
    }
}
