using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Contracts;
using Trap_Intel.Application.Users.Queries.GetUsers;
using Trap_Intel.Application.Users.Queries.GetUserById;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.Users;

internal sealed class GetUsersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/", GetUsers)
            .WithName("GetUsers")
            .WithSummary("List all users in organization")
            .WithDescription("Retrieve paginated list of users with filtering options")
            .RequirePermission(Permissions.Users.View)
            .Produces<PagedResult<UserDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{userId:guid}", GetUserById)
            .WithName("GetUserById")
            .WithSummary("Get user details")
            .RequirePermission(Permissions.Users.View)
            .Produces<UserDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetUsers(
        [AsParameters] GlobalListQueryRequest listQuery,
        [FromQuery] string? searchTerm,
        [FromQuery] UserStatus? status,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation retrieves paginated list of users
    }

    private static async Task<IResult> GetUserById(
        Guid userId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation retrieves detailed user information
    }
}
