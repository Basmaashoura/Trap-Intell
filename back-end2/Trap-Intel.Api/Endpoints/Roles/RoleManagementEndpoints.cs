using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Application.Roles.Commands.AddRolePermission;
using Trap_Intel.Application.Roles.Commands.CreateRole;
using Trap_Intel.Application.Roles.Commands.DeleteRole;
using Trap_Intel.Application.Roles.Commands.RemoveRolePermission;
using Trap_Intel.Application.Roles.Commands.SetRolePermissions;
using Trap_Intel.Application.Roles.Commands.UpdateRole;
using Trap_Intel.Application.Roles.Queries.GetRoleById;
using Trap_Intel.Application.Roles.Queries.GetRoles;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
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
            .WithSummary("Creates a custom role for the current organization")
            .RequirePermission(Permissions.Users.ManageRoles)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{roleId:guid}", GetRoleById)
            .WithName("GetRoleById")
            .WithSummary("Retrieves a role by ID")
            .RequirePermission(Permissions.Users.View)
            .Produces<RoleDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/{roleId:guid}", UpdateRole)
            .WithName("UpdateRole")
            .WithSummary("Updates custom role metadata and optional active state")
            .RequirePermission(Permissions.Users.ManageRoles)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/{roleId:guid}/permissions", SetPermissions)
            .WithName("SetRolePermissions")
            .WithSummary("Replaces all permissions for a custom role")
            .RequirePermission(Permissions.Users.ManageRoles)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{roleId:guid}/permissions", AddPermission)
            .WithName("AddRolePermission")
            .WithSummary("Adds a permission to a custom role in the current organization")
            .RequirePermission(Permissions.Users.ManageRoles)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{roleId:guid}/permissions/{permission}", RemovePermission)
            .WithName("RemoveRolePermission")
            .WithSummary("Removes a permission from a custom role in the current organization")
            .RequirePermission(Permissions.Users.ManageRoles)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{roleId:guid}", DeleteRole)
            .WithName("DeleteRole")
            .WithSummary("Deletes a custom role if it has no assigned users")
            .RequirePermission(Permissions.Users.ManageRoles)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> CreateRole(
        [FromBody] CreateRoleRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(httpContext, out var organizationId))
            return Results.Forbid();

        var command = new CreateRoleCommand(
            organizationId,
            request.Name,
            request.Description ?? string.Empty,
            request.Permissions ?? Array.Empty<string>());

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to create role",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Created($"/api/roles/{result.Value}", new
        {
            roleId = result.Value,
            message = "Role created successfully."
        });
    }

    private static async Task<IResult> AddPermission(
        Guid roleId,
        [FromBody] AddRolePermissionRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(httpContext, out var organizationId))
            return Results.Forbid();

        var command = new AddRolePermissionCommand(roleId, organizationId, request.Permission);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();

            if (error?.Code == "Role.NotFound")
                return Results.NotFound(new { message = error.Message });

            if (error?.Code == "Role.ScopeViolation")
                return Results.Forbid();

            return Results.Problem(
                title: "Failed to add permission",
                detail: error?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Permission added successfully." });
    }

    private static async Task<IResult> GetRoleById(
        Guid roleId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken,
        [FromQuery] Guid? organizationId = null)
    {
        if (!TryResolveReadableOrganizationId(httpContext, organizationId, out var targetOrganizationId))
            return Results.Forbid();

        var result = await sender.Send(new GetRoleByIdQuery(roleId, targetOrganizationId), cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();

            if (error?.Code == "Role.NotFound")
                return Results.NotFound(new { message = error.Message });

            if (error?.Code == "Role.ScopeViolation")
                return Results.Forbid();

            return Results.Problem(
                title: "Failed to retrieve role",
                detail: error?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> UpdateRole(
        Guid roleId,
        [FromBody] UpdateRoleRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(httpContext, out var organizationId))
            return Results.Forbid();

        var command = new UpdateRoleCommand(
            roleId,
            organizationId,
            request.Name,
            request.Description ?? string.Empty,
            request.IsActive);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();

            if (error?.Code == "Role.NotFound")
                return Results.NotFound(new { message = error.Message });

            if (error?.Code == "Role.ScopeViolation")
                return Results.Forbid();

            return Results.Problem(
                title: "Failed to update role",
                detail: error?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Role updated successfully." });
    }

    private static async Task<IResult> SetPermissions(
        Guid roleId,
        [FromBody] SetRolePermissionsRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(httpContext, out var organizationId))
            return Results.Forbid();

        var command = new SetRolePermissionsCommand(
            roleId,
            organizationId,
            request.Permissions ?? Array.Empty<string>());

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();

            if (error?.Code == "Role.NotFound")
                return Results.NotFound(new { message = error.Message });

            if (error?.Code == "Role.ScopeViolation")
                return Results.Forbid();

            return Results.Problem(
                title: "Failed to update role permissions",
                detail: error?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Role permissions updated successfully." });
    }

    private static async Task<IResult> RemovePermission(
        Guid roleId,
        string permission,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(httpContext, out var organizationId))
            return Results.Forbid();

        var decodedPermission = Uri.UnescapeDataString(permission);
        var command = new RemoveRolePermissionCommand(roleId, organizationId, decodedPermission);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();

            if (error?.Code == "Role.NotFound")
                return Results.NotFound(new { message = error.Message });

            if (error?.Code == "Role.ScopeViolation")
                return Results.Forbid();

            return Results.Problem(
                title: "Failed to remove permission",
                detail: error?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Permission removed successfully." });
    }

    private static async Task<IResult> DeleteRole(
        Guid roleId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganizationId(httpContext, out var organizationId))
            return Results.Forbid();

        var command = new DeleteRoleCommand(roleId, organizationId);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();

            if (error?.Code == "Role.NotFound")
                return Results.NotFound(new { message = error.Message });

            if (error?.Code == "Role.ScopeViolation")
                return Results.Forbid();

            return Results.Problem(
                title: "Failed to delete role",
                detail: error?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Role deleted successfully." });
    }

    private static bool TryGetOrganizationId(HttpContext httpContext, out Guid organizationId)
    {
        var organizationClaim = httpContext.User.GetOrganizationClaimValue();
        return Guid.TryParse(organizationClaim, out organizationId);
    }

    private static bool TryResolveReadableOrganizationId(
        HttpContext httpContext,
        Guid? requestedOrganizationId,
        out Guid? targetOrganizationId)
    {
        targetOrganizationId = null;

        if (httpContext.User.IsSuperAdmin())
        {
            if (requestedOrganizationId.HasValue)
            {
                targetOrganizationId = requestedOrganizationId.Value;
                return true;
            }

            if (httpContext.User.TryGetOrganizationId(out var claimOrganizationId))
            {
                targetOrganizationId = claimOrganizationId;
            }

            return true;
        }

        if (!httpContext.User.TryGetOrganizationId(out var callerOrganizationId))
            return false;

        if (requestedOrganizationId.HasValue && requestedOrganizationId.Value != callerOrganizationId)
            return false;

        targetOrganizationId = callerOrganizationId;
        return true;
    }
}

public sealed record CreateRoleRequest(
    string Name,
    string? Description,
    IReadOnlyCollection<string>? Permissions);

public sealed record AddRolePermissionRequest(string Permission);

public sealed record UpdateRoleRequest(
    string Name,
    string? Description,
    bool? IsActive);

public sealed record SetRolePermissionsRequest(
    IReadOnlyCollection<string>? Permissions);
