using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trap_Intel.Application.Notifications.Commands.RegisterPushToken;
using Trap_Intel.Application.Notifications.Commands.DeletePushToken;
using Trap_Intel.Api.Endpoints.Notifications.Models;
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Notifications.Enums;

namespace Trap_Intel.Api.Endpoints.Notifications;

internal sealed class PushTokenEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications/push-tokens")
            .WithTags("Notification Settings")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapPost("/", RegisterToken)
            .WithName("RegisterPushToken")
            .WithSummary("Registers a new device push token for the user")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapDelete("/{token}", DeleteToken)
            .WithName("DeletePushToken")
            .WithSummary("Removes a registered device push token")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/", DeleteTokenByQuery)
            .WithName("DeletePushTokenByQuery")
            .WithSummary("Removes a registered device push token using query parameter")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static Task<IResult> DeleteTokenByQuery(
        [FromQuery] string token,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return DeleteToken(token, sender, httpContext, cancellationToken);
    }

    private static async Task<IResult> RegisterToken(
        [FromBody] RegisterPushTokenRequest request,
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Results.Unauthorized();

        var command = new RegisterPushTokenCommand(
            userId, 
            request.Token, 
            (PushPlatform)request.Platform, 
            request.DeviceId);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(title: "Failed to register push token", detail: result.Errors.FirstOrDefault()?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Push token registered successfully." });
    }

    private static async Task<IResult> DeleteToken(
        string token,
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Results.BadRequest(new { message = "Token is required." });

        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Results.Unauthorized();

        var result = await sender.Send(new DeletePushTokenCommand(userId, Uri.UnescapeDataString(token)), cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            if (error?.Code == "Notification.TokenNotFound")
                return Results.NotFound(new { message = error.Message });

            return Results.Problem(title: "Failed to delete push token", detail: error?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Push token deleted successfully." });
    }
}
