using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trap_Intel.Application.Notifications.Commands.MarkAsRead;
using Trap_Intel.Application.Notifications.Commands.MarkAllAsRead;
using Trap_Intel.Api.Filters;

namespace Trap_Intel.Api.Endpoints.Notifications;

internal sealed class NotificationActionEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapPut("/{notificationId:guid}/read", MarkAsRead)
            .WithName("MarkNotificationAsRead")
            .WithSummary("Marks a specific notification as read")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/read-all", MarkAllAsRead)
            .WithName("MarkAllNotificationsAsRead")
            .WithSummary("Marks all user notifications as read")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> MarkAsRead(
        Guid notificationId,
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Results.Unauthorized();

        var result = await sender.Send(new MarkAsReadCommand(userId, notificationId), cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            if (error?.Code == "Notification.NotFound")
                return Results.NotFound(new { message = error.Message });

            return Results.Problem(title: "Update Failed", detail: error?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Notification marked as read." });
    }

    private static async Task<IResult> MarkAllAsRead(
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Results.Unauthorized();

        var result = await sender.Send(new MarkAllAsReadCommand(userId), cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(title: "Update Failed", detail: result.Errors.FirstOrDefault()?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "All notifications marked as read." });
    }
}
