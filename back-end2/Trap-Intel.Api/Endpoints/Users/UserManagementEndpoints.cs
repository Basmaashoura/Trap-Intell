using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trap_Intel.Application.Users.Commands.DeactivateUser;
using Trap_Intel.Application.Users.Commands.ChangeUserRole;
using Trap_Intel.Application.Users.Commands.SuspendUser;
using Trap_Intel.Application.Users.Commands.UnsuspendUser;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Domain.Roles;
using System.ComponentModel.DataAnnotations;

namespace Trap_Intel.Api.Endpoints.Users;

internal sealed class UserManagementEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization(); // Might require stronger policies later (e.g. AdminOnly)

        group.MapPost("/{userId:guid}/deactivate", DeactivateUser)
            .WithName("DeactivateUser")
            .WithSummary("Deactivates a user, preventing login")
            .RequirePermission(Permissions.Users.Update)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{userId:guid}/role", ChangeUserRole)
            .WithName("ChangeUserRole")
            .WithSummary("Changes the functional role of a user")
            .RequirePermission(Permissions.Users.ManageRoles)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{userId:guid}/suspend", SuspendUser)
            .WithName("SuspendUser")
            .WithSummary("Suspend a user")
            .WithDescription("Suspends a user temporarily. All sessions will be revoked from domain events.")
            .RequirePermission(Permissions.Users.Update)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{userId:guid}/unsuspend", UnsuspendUser)
            .WithName("UnsuspendUser")
            .WithSummary("Unsuspend a user")
            .WithDescription("Removes the suspension restriction from a user.")
            .RequirePermission(Permissions.Users.Update)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> DeactivateUser(
        Guid userId,
        [FromBody] DeactivateUserRequest request,
        IUserRepository userRepository,
        HttpContext httpContext,
        ISender sender, 
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(httpContext, out var currentUserId))
            return Results.Unauthorized();

        if (currentUserId == userId)
        {
            return Results.Problem(
                title: "Invalid Operation",
                detail: "You cannot deactivate your own account.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var targetUser = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (targetUser is null)
            return Results.NotFound();

        if (!IsSameOrganizationOrSuperAdmin(httpContext, targetUser.OrganizationId))
            return Results.Forbid();

        var command = new DeactivateUserCommand(userId, request.Reason);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to deactivate user",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest); // Use 400 or 404 depending on the error
        }

        return Results.Ok(new { message = "User deactivated successfully." });
    }

    private static async Task<IResult> ChangeUserRole(
        Guid userId,
        [FromBody] ChangeUserRoleRequest request,
        IUserRepository userRepository,
        HttpContext httpContext,
        ISender sender, 
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(httpContext, out var currentUserId))
            return Results.Unauthorized();

        if (currentUserId == userId)
        {
            return Results.Problem(
                title: "Invalid Operation",
                detail: "You cannot change your own role.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var targetUser = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (targetUser is null)
            return Results.NotFound();

        if (!IsSameOrganizationOrSuperAdmin(httpContext, targetUser.OrganizationId))
            return Results.Forbid();

        var roleClaim = httpContext.User.FindFirst(ClaimTypes.Role)?.Value
            ?? httpContext.User.FindFirst("role")?.Value;

        if (!SystemRoles.TryResolveRoleId(roleClaim, out var assignerRoleId))
            return Results.Forbid();

        if (!RolePermissionMap.CanAssignRole(assignerRoleId, request.RoleId))
            return Results.Forbid();

        var command = new ChangeUserRoleCommand(userId, request.RoleId);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to change user role",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "User role updated successfully." });
    }

    private static async Task<IResult> SuspendUser(
        Guid userId,
        [FromBody] SuspendUserRequest request,
        IUserRepository userRepository,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(httpContext, out var currentUserId))
            return Results.Unauthorized();

        if (currentUserId == userId)
        {
            return Results.Problem(
                title: "Invalid Operation",
                detail: "You cannot suspend your own account.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var targetUser = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (targetUser is null)
            return Results.NotFound(new { message = "User not found." });

        if (!IsSameOrganizationOrSuperAdmin(httpContext, targetUser.OrganizationId))
            return Results.Forbid();

        var command = new SuspendUserCommand(userId, request.Reason ?? "Administrative suspension");
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Any(e => e.Code == "Identity.UserNotFound") 
                ? Results.NotFound(new { message = result.Errors.First().Message })
                : Results.Problem(title: "Failed to suspend user", detail: result.Errors.FirstOrDefault()?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "User has been suspended successfully." });
    }

    private static async Task<IResult> UnsuspendUser(
        Guid userId,
        IUserRepository userRepository,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(httpContext, out _))
            return Results.Unauthorized();

        var targetUser = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (targetUser is null)
            return Results.NotFound(new { message = "User not found." });

        if (!IsSameOrganizationOrSuperAdmin(httpContext, targetUser.OrganizationId))
            return Results.Forbid();

        var command = new UnsuspendUserCommand(userId);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Any(e => e.Code == "Identity.UserNotFound") 
                ? Results.NotFound(new { message = result.Errors.First().Message })
                : Results.Problem(title: "Failed to unsuspend user", detail: result.Errors.FirstOrDefault()?.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "User has been unsuspended successfully." });
    }

    private static bool TryGetCurrentUserId(HttpContext httpContext, out Guid userId)
    {
        var claimValue = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");

        return Guid.TryParse(claimValue, out userId);
    }

    private static bool IsSameOrganizationOrSuperAdmin(HttpContext httpContext, Guid targetOrganizationId)
    {
        if (httpContext.User.IsSuperAdmin())
            return true;

        var organizationClaim = httpContext.User.GetOrganizationClaimValue();
        return Guid.TryParse(organizationClaim, out var callerOrganizationId)
            && callerOrganizationId == targetOrganizationId;
    }
}

public sealed record DeactivateUserRequest(string Reason);
public sealed record ChangeUserRoleRequest(Guid RoleId);
public sealed record SuspendUserRequest(string? Reason);
