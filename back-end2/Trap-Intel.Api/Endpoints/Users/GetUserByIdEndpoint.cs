using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Application.Users.Queries.GetUserById;
using Trap_Intel.Application.Users.Queries.GetUsers; // UserDto
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.Users;

internal sealed class GetUserByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/{userId:guid}", HandleAsync)
            .WithName("GetUserById")
            .WithSummary("Retrieves a user by ID")
            .RequirePermission(Permissions.Users.View)
            .Produces<UserDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        Guid userId,
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserByIdQuery(userId), cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "User Not Found",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status404NotFound);
        }

        if (!httpContext.User.IsAuthorizedForOrganization(result.Value.OrganizationId))
        {
            // Hide cross-organization existence details.
            return Results.NotFound();
        }

        return Results.Ok(result.Value);
    }
}
