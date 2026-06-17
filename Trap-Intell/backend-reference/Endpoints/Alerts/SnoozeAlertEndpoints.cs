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

        group.MapPost("/{alertId:guid}/snooze", SnoozeAlert)
            .WithName("SnoozeAlert")
            .WithSummary("Temporarily snooze an alert for specified duration")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{alertId:guid}/unsnooze", UnsnoozeAlert)
            .WithName("UnsnoozeAlert")
            .WithSummary("Resume a snoozed alert immediately")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> SnoozeAlert(
        Guid organizationId,
        Guid alertId,
        [FromBody] SnoozeAlertCommand command,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation snoozes alert for specified duration
    }

    private static async Task<IResult> UnsnoozeAlert(
        Guid organizationId,
        Guid alertId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation resumes a snoozed alert
    }
}
