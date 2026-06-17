using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trap_Intel.Application.Users.Commands.DeactivateUser;
using Trap_Intel.Application.Users.Commands.ChangeUserRole;
using Trap_Intel.Application.Users.Commands.SuspendUser;
using Trap_Intel.Application.Users.Commands.UnsuspendUser;
using Trap_Intel.Api.Filters;

namespace Trap_Intel.Api.Endpoints.Users;

internal sealed class UserManagementEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapPut("/{userId:guid}/role", ChangeRole)
            .WithName("ChangeUserRole")
            .WithSummary("Change user role within organization")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{userId:guid}/deactivate", Deactivate)
            .WithName("DeactivateUser")
            .WithSummary("Deactivate user account")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{userId:guid}/suspend", Suspend)
            .WithName("SuspendUser")
            .WithSummary("Suspend user account")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{userId:guid}/unsuspend", Unsuspend)
            .WithName("UnsuspendUser")
            .WithSummary("Unsuspend user account")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ChangeRole(
        Guid userId,
        [FromBody] ChangeUserRoleCommand command,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation changes user role
    }

    private static async Task<IResult> Deactivate(
        Guid userId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation deactivates user account
    }

    private static async Task<IResult> Suspend(
        Guid userId,
        [FromBody] SuspendUserCommand command,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation suspends user account
    }

    private static async Task<IResult> Unsuspend(
        Guid userId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation unsuspends user account
    }
}
