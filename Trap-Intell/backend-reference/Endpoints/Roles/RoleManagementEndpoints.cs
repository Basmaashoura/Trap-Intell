using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Application.Roles.Commands.AddRolePermission;
using Trap_Intel.Application.Roles.Commands.CreateRole;
using Trap_Intel.Application.Roles.Commands.DeleteRole;
using Trap_Intel.Application.Roles.Commands.RemoveRolePermission;
using Trap_Intel.Application.Roles.Commands.SetRolePermissions;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.Roles;

internal sealed class RoleManagementEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roles")
            .WithTags("Roles & Permissions")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapPost("/", CreateRole)
            .WithName("CreateRole")
            .WithSummary("Create new role")
            .RequirePermission(Permissions.Users.ManageRoles)
            .Produces<RoleDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapDelete("/{roleId:guid}", DeleteRole)
            .WithName("DeleteRole")
            .WithSummary("Delete role")
            .RequirePermission(Permissions.Users.ManageRoles)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{roleId:guid}/permissions", SetPermissions)
            .WithName("SetRolePermissions")
            .WithSummary("Set all permissions for role")
            .RequirePermission(Permissions.Users.ManageRoles)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{roleId:guid}/permissions/add", AddPermission)
            .WithName("AddRolePermission")
            .WithSummary("Add permission to role")
            .RequirePermission(Permissions.Users.ManageRoles)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{roleId:guid}/permissions/{permission}", RemovePermission)
            .WithName("RemoveRolePermission")
            .WithSummary("Remove permission from role")
            .RequirePermission(Permissions.Users.ManageRoles)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateRole(
        [FromBody] CreateRoleCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Implementation creates new role
    }

    private static async Task<IResult> DeleteRole(
        Guid roleId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Implementation deletes role
    }

    private static async Task<IResult> SetPermissions(
        Guid roleId,
        [FromBody] SetRolePermissionsCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Implementation sets all permissions for role
    }

    private static async Task<IResult> AddPermission(
        Guid roleId,
        [FromBody] AddRolePermissionCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Implementation adds permission to role
    }

    private static async Task<IResult> RemovePermission(
        Guid roleId,
        string permission,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Implementation removes permission from role
    }
}
