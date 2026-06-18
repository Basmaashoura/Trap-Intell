using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trap_Intel.Api.Contracts;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Application.Notifications.Queries.GetNotifications;
using Trap_Intel.Application.Notifications.Queries.GetUnreadCount;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Notifications.Enums;

namespace Trap_Intel.Api.Endpoints.Notifications;

internal sealed class GetNotificationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/", HandleGetNotifications)
            .WithName("GetNotifications")
            .WithSummary("Retrieves notifications for the current user")
            .Produces<PagedResult<NotificationDto>>(StatusCodes.Status200OK);

        group.MapGet("/unread-count", HandleGetUnreadCount)
            .WithName("GetUnreadCount")
            .WithSummary("Retrieves the count of unread notifications")
            .Produces<int>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleGetNotifications(
        [AsParameters] GlobalListQueryRequest listQuery,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] NotificationCategory? category = null,
        [FromQuery] NotificationPriority? priority = null)
    {
        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Results.Unauthorized();

        var query = new GetNotificationsQuery(
            userId,
            unreadOnly,
            category,
            priority,
            listQuery.ToQueryOptions());
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to retrieve notifications",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        var filterKey = listQuery.BuildFilterKey(
            ("unread", unreadOnly),
            ("category", category),
            ("priority", priority));

        httpContext.Response.SetListRealtimeHeaders("notifications", "user", filterKey);

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> HandleGetUnreadCount(
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Results.Unauthorized();

        var result = await sender.Send(new GetUnreadCountQuery(userId), cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(title: "Failed", detail: result.Errors.FirstOrDefault()?.Message);
        }

        return Results.Ok(new { count = result.Value });
    }
}
