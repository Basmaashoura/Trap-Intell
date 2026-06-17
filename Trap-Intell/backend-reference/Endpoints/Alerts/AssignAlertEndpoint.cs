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
        [FromBody] AssignAlertCommand command,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation assigns alert to specified user
    }
}
