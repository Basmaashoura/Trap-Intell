using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Filters;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Domain.Notifications;
using Trap_Intel.Domain.Notifications.Enums;
using NotificationCategory = Trap_Intel.Domain.Notifications.Enums.NotificationCategory;
using NotificationPriority = Trap_Intel.Domain.Notifications.Enums.NotificationPriority;

namespace Trap_Intel.Api.Endpoints.Notifications;

internal sealed class NotificationDebugEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications/debug")
            .WithTags("Notifications")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapPost("/send-self", SendSelf)
            .WithName("SendDebugNotificationToSelf")
            .WithSummary("Development-only endpoint to dispatch a test notification to current user")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/send-self-standard", SendSelfStandard)
            .WithName("SendStandardNotificationToSelf")
            .WithSummary("Development-only endpoint to dispatch a configurable non-debug notification to current user")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> SendSelf(
        INotificationDispatcher dispatcher,
        HttpContext httpContext,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !string.Equals(environment.EnvironmentName, "Docker", StringComparison.OrdinalIgnoreCase))
        {
            return Results.NotFound();
        }

        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Results.Unauthorized();
        }

        var notificationResult = Notification.Create(
            userId: userId,
            type: "DebugRealtimeTest",
            title: "Realtime Debug Notification",
            message: $"SignalR dispatch test at {DateTime.UtcNow:O}",
            category: NotificationCategory.System,
            priority: NotificationPriority.Normal);

        if (notificationResult.IsFailure)
        {
            return Results.Problem(
                title: "Failed to create debug notification",
                detail: notificationResult.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        await dispatcher.DispatchAsync(notificationResult.Value, cancellationToken);

        return Results.Ok(new
        {
            notificationId = notificationResult.Value.Id,
            message = "Debug notification dispatched."
        });
    }

    private static async Task<IResult> SendSelfStandard(
        INotificationDispatcher dispatcher,
        HttpContext httpContext,
        IHostEnvironment environment,
        [FromQuery] string? type,
        [FromQuery] string? title,
        [FromQuery] string? message,
        [FromQuery] NotificationCategory? category,
        [FromQuery] NotificationPriority? priority,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !string.Equals(environment.EnvironmentName, "Docker", StringComparison.OrdinalIgnoreCase))
        {
            return Results.NotFound();
        }

        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Results.Unauthorized();
        }

        var normalizedType = string.IsNullOrWhiteSpace(type) ? "Maintenance" : type.Trim();
        var normalizedTitle = string.IsNullOrWhiteSpace(title) ? "Standard Notification Test" : title.Trim();
        var normalizedMessage = string.IsNullOrWhiteSpace(message)
            ? $"Standard dispatch test at {DateTime.UtcNow:O}"
            : message.Trim();

        var notificationResult = Notification.Create(
            userId: userId,
            type: normalizedType,
            title: normalizedTitle,
            message: normalizedMessage,
            category: category ?? NotificationCategory.System,
            priority: priority ?? NotificationPriority.Normal);

        if (notificationResult.IsFailure)
        {
            return Results.Problem(
                title: "Failed to create standard notification",
                detail: notificationResult.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        await dispatcher.DispatchAsync(notificationResult.Value, cancellationToken);

        return Results.Ok(new
        {
            notificationId = notificationResult.Value.Id,
            notificationType = normalizedType,
            category = category ?? NotificationCategory.System,
            priority = priority ?? NotificationPriority.Normal,
            message = "Standard notification dispatched."
        });
    }
}