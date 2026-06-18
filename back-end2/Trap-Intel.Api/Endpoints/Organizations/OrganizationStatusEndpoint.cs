using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trap_Intel.Application.Organizations;
using Trap_Intel.Application.Organizations.Commands.UpdateOrganizationStatus;
using Trap_Intel.Application.Organizations.Queries.GetOrganizationStatus;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Organizations;

namespace Trap_Intel.Api.Endpoints.Organizations;

internal sealed class OrganizationStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}")
            .WithTags("Organizations")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/status", GetStatus)
            .WithName("GetOrganizationStatus")
            .WithSummary("Gets the current status of an organization")
            .Produces<OrganizationStatusDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPut("/status", UpdateStatus)
            .WithName("UpdateOrganizationStatus")
            .WithSummary("Updates organization status (Active, Suspended, Inactive)")
            .Produces<OrganizationStatusDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetStatus(
        Guid organizationId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorizedForOrganization(httpContext, organizationId))
        {
            return Results.Forbid();
        }

        var result = await sender.Send(new GetOrganizationStatusQuery(organizationId), cancellationToken);

        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var statusCode = firstError?.Code == "Organization.NotFound"
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

            return Results.Problem(
                title: "Failed to retrieve organization status",
                detail: firstError?.Message,
                statusCode: statusCode,
                instance: httpContext.Request.Path);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> UpdateStatus(
        Guid organizationId,
        [FromBody] Models.UpdateOrganizationStatusRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorizedForOrganization(httpContext, organizationId))
        {
            return Results.Forbid();
        }

        var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var changedByUserId))
        {
            return Results.Unauthorized();
        }

        if (!Enum.TryParse<OrganizationStatus>(request.Status, ignoreCase: true, out var targetStatus))
        {
            return Results.Problem(
                title: "Invalid status",
                detail: "Allowed status values are: Active, Suspended, Inactive.",
                statusCode: StatusCodes.Status400BadRequest,
                instance: httpContext.Request.Path);
        }

        if (targetStatus == OrganizationStatus.PendingApproval)
        {
            return Results.Problem(
                title: "Invalid status transition target",
                detail: "PendingApproval cannot be set directly from this endpoint.",
                statusCode: StatusCodes.Status400BadRequest,
                instance: httpContext.Request.Path);
        }

        var command = new UpdateOrganizationStatusCommand(
            organizationId,
            targetStatus,
            changedByUserId,
            request.Reason);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var statusCode = firstError?.Code == "Organization.NotFound"
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

            return Results.Problem(
                title: "Failed to update organization status",
                detail: firstError?.Message,
                statusCode: statusCode,
                instance: httpContext.Request.Path);
        }

        return Results.Ok(result.Value);
    }

    private static bool IsAuthorizedForOrganization(HttpContext httpContext, Guid organizationId)
    {
        return httpContext.User.IsAuthorizedForOrganization(organizationId);
    }
}
