using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trap_Intel.Application.Users.Commands.UpdateNotificationSettings;
using Trap_Intel.Domain.Identity.Notifications;
using Trap_Intel.Api.Filters;

namespace Trap_Intel.Api.Endpoints.Notifications;

internal sealed class NotificationSettingsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications/settings")
            .WithTags("Notification Settings")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapPut("/", UpdateSettings)
            .WithName("UpdateNotificationSettings")
            .WithSummary("Updates notification preferences for the authenticated user")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> UpdateSettings(
        [FromBody] UserNotificationSettings settings,
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Results.Unauthorized();

        var result = await sender.Send(new UpdateNotificationSettingsCommand(userId, settings), cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to update notification settings",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Notification settings updated successfully." });
    }
}
