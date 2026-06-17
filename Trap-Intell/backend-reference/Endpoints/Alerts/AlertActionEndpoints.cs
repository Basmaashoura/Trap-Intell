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
            .WithSummary("Mark an alert as acknowledged")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{alertId:guid}/resolve", ResolveAlert)
            .WithName("ResolveAlert")
            .WithSummary("Mark an alert as resolved")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> AcknowledgeAlert(
        Guid organizationId,
        Guid alertId,
        [FromBody] AcknowledgeAlertCommand command,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation marks alert as acknowledged
    }

    private static async Task<IResult> ResolveAlert(
        Guid organizationId,
        Guid alertId,
        [FromBody] ResolveAlertCommand command,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation marks alert as resolved
    }
}
