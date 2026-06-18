using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Application.Roles.Queries.GetPermissions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.Roles;

internal sealed class GetPermissionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roles")
            .WithTags("Roles & Permissions")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/permissions", HandleAsync)
            .WithName("GetPermissions")
            .WithSummary("Retrieves all granular permissions available in the system")
            .RequirePermission(Permissions.Users.ManageRoles)
            .Produces<IEnumerable<string>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPermissionsQuery(), cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to retrieve permissions",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result.Value);
    }
}
