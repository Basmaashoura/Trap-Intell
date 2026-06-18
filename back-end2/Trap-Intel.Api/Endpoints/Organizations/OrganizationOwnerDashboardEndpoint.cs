using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Application.Organizations.Queries.GetOrganizationOwnerDashboard;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.Organizations;

internal sealed class OrganizationOwnerDashboardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/dashboard")
            .WithTags("Organizations")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/owner", GetOwnerDashboard)
            .WithName("GetOrganizationOwnerDashboard")
            .WithSummary("Gets owner-focused organization dashboard metrics")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<OrganizationOwnerDashboardDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetOwnerDashboard(
        Guid organizationId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        [FromQuery] int lastNDays = 30)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var result = await sender.Send(
            new GetOrganizationOwnerDashboardQuery(organizationId, lastNDays),
            cancellationToken);

        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var statusCode = string.Equals(firstError?.Code, "Organization.NotFound", StringComparison.OrdinalIgnoreCase)
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

            return Results.Problem(
                title: "Failed to retrieve owner dashboard",
                detail: firstError?.Message,
                statusCode: statusCode,
                instance: httpContext.Request.Path);
        }

        return Results.Ok(result.Value);
    }
}
