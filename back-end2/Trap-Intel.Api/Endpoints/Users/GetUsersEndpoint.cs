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
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/users")
            .WithTags("Users")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/", HandleAsync)
            .WithName("GetOrganizationUsers")
            .WithSummary("Retrieves all users for a specified organization")
            .RequirePermission(Permissions.Users.View)
            .Produces<PagedResult<UserDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{userId:guid}", HandleUserByIdAsync)
            .WithName("GetOrganizationUserById")
            .WithSummary("Retrieves a specific user within an organization")
            .RequirePermission(Permissions.Users.View)
            .Produces<UserDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        Guid organizationId,
        ISender sender, 
        HttpContext httpContext,
        CancellationToken cancellationToken,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = GlobalQueryOptions.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "desc",
        [FromQuery] UserStatus? status = null,
        [FromQuery] Guid? roleId = null)
    {
        var listQuery = new GlobalListQueryRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        if (!httpContext.User.IsAuthorizedForOrganization(organizationId, allowSuperAdminBypass: false))
            return Results.Forbid();

        var result = await sender.Send(
            new GetUsersQuery(organizationId, status, roleId, listQuery.ToQueryOptions()),
            cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to retrieve users",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        var filterKey = listQuery.BuildFilterKey(
            ("status", status),
            ("roleid", roleId));

        httpContext.Response.SetListRealtimeHeaders("users", "organization", filterKey);

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> HandleUserByIdAsync(
        Guid organizationId,
        Guid userId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId, allowSuperAdminBypass: false))
            return Results.Forbid();

        var result = await sender.Send(new GetUserByIdQuery(userId), cancellationToken);

        if (result.IsFailure)
        {
            return Results.NotFound(new { message = "User not found." });
        }

        if (result.Value.OrganizationId != organizationId)
        {
            return Results.NotFound(new { message = "User not found." });
        }

        return Results.Ok(result.Value);
    }
}
