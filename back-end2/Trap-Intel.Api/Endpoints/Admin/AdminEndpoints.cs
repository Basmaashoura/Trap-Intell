using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Application.Billing.Commands.GenerateMonthlyInvoices;
using Trap_Intel.Application.Users.Commands.SuspendUser;
using Trap_Intel.Application.Users.Commands.UnsuspendUser;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Application.Billing.Commands.ProcessOverdueInvoices;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Infrastructure.Authorization;

namespace Trap_Intel.Api.Endpoints;

/// <summary>
/// Admin endpoints for user and organization management.
/// All endpoints require appropriate authorization policies.
/// </summary>
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Administration")
            .WithOpenApi()
            .RequireAuthorization();

        #region User Management

        group.MapGet("/users", GetOrganizationUsers)
            .WithName("AdminGetOrganizationUsers")
            .WithSummary("Get all users in the current organization")
            .RequirePermission(Permissions.Users.View)
            .Produces<OrganizationUsersResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/users/{userId:guid}", GetUserById)
            .WithName("AdminGetUserById")
            .WithSummary("Get a specific user by ID")
            .RequirePermission(Permissions.Users.View)
            .Produces<AdminUserInfo>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/users/{userId:guid}/change-role", ChangeUserRole)
            .WithName("AdminChangeUserRole")
            .WithSummary("Change a user's role within the organization")
            .RequirePermission(Permissions.Users.ManageRoles)
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/users/{userId:guid}/deactivate", DeactivateUser)
            .WithName("AdminDeactivateUser")
            .WithSummary("Deactivate a user account")
            .RequirePermission(Permissions.Users.Update)
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/users/{userId:guid}/activate", ActivateUser)
            .WithName("AdminActivateUser")
            .WithSummary("Activate a user account")
            .RequirePermission(Permissions.Users.Update)
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/users/{userId:guid}/unlock", UnlockUser)
            .WithName("AdminUnlockUser")
            .WithSummary("Unlock a locked user account")
            .RequirePermission(Permissions.Users.Update)
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/users/{userId:guid}/suspend", SuspendUser)
            .WithName("AdminSuspendUser")
            .WithSummary("Suspend a user account")
            .WithDescription("Temporarily suspends a user and revokes active sessions through domain events.")
            .RequirePermission(Permissions.Users.Update)
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/users/{userId:guid}/unsuspend", UnsuspendUser)
            .WithName("AdminUnsuspendUser")
            .WithSummary("Unsuspend a user account")
            .WithDescription("Removes suspension restrictions from a user.")
            .RequirePermission(Permissions.Users.Update)
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        #endregion

        #region Billing Operations

        group.MapPost("/billing/invoices/overdue/process", ProcessOverdueInvoices)
            .WithName("AdminProcessOverdueInvoices")
            .WithSummary("Processes overdue invoices with optional dry-run and late-fee settings")
            .RequireSuperAdmin()
            .Produces<OverdueInvoiceProcessingResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/billing/invoices/monthly/generate", GenerateMonthlyInvoices)
            .WithName("AdminGenerateMonthlyInvoices")
            .WithSummary("Generates monthly invoices with optional dry-run mode")
            .RequireSuperAdmin()
            .Produces<MonthlyInvoiceGenerationResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        #endregion

        #region My Permissions

        group.MapGet("/permissions/me", GetMyPermissions)
            .WithName("AdminGetMyPermissions")
            .WithSummary("Get all permissions for the current user")
            .Produces<UserPermissionsResponse>(StatusCodes.Status200OK);

        group.MapGet("/permissions/roles", GetAllRolePermissions)
            .WithName("AdminGetAllRolePermissions")
            .WithSummary("Get permissions matrix for all roles")
            .RequireSuperAdmin()
            .Produces<RolePermissionsMatrixResponse>(StatusCodes.Status200OK);

        #endregion

        return app;
    }

    #region Handlers

    private static async Task<IResult> GetOrganizationUsers(
        IUserRepository userRepository,
        Trap_Intel.Domain.Roles.IRoleRepository roleRepository,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var orgId = GetOrganizationId(httpContext);
        if (orgId is null)
            return Results.Unauthorized();

        logger.LogInformation("Listing users for organization {OrgId}", orgId);

        var users = await userRepository.GetByOrganizationAsync(orgId.Value, cancellationToken);

        var result = new List<AdminUserInfo>(users.Count);
        foreach (var user in users)
        {
            result.Add(await MapToAdminUserInfoAsync(user, roleRepository, cancellationToken));
        }

        return Results.Ok(new OrganizationUsersResponse
        {
            Users = result,
            TotalCount = result.Count
        });
    }

    private static async Task<IResult> GetUserById(
        Guid userId,
        IUserRepository userRepository,
        Trap_Intel.Domain.Roles.IRoleRepository roleRepository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var orgId = GetOrganizationId(httpContext);
        if (orgId is null)
            return Results.Unauthorized();

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Results.NotFound();

        // Ensure same org unless SuperAdmin
        var role = GetRoleClaimValue(httpContext);
        if (user.OrganizationId != orgId.Value
            && !Trap_Intel.Domain.Roles.SystemRoles.IsSuperAdmin(role))
        {
            return Results.NotFound();
        }

        return Results.Ok(await MapToAdminUserInfoAsync(user, roleRepository, cancellationToken));
    }

    private static async Task<IResult> ChangeUserRole(
        Guid userId,
        ChangeRoleRequest request,
        IUserRepository userRepository,
        Domain.Abstractions.IUnitOfWork unitOfWork,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetUserId(httpContext);
        var orgId = GetOrganizationId(httpContext);
        if (currentUserId is null || orgId is null)
            return Results.Unauthorized();

        if (currentUserId == userId)
        {
            return Results.Problem(
                title: "Invalid Operation",
                detail: "You cannot change your own role.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!Guid.TryParse(request.NewRole, out var newRoleId))
        {
            return Results.Problem(
                title: "Invalid Role",
                detail: $"'{request.NewRole}' is not a valid role ID.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var targetUser = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (targetUser is null || targetUser.OrganizationId != orgId.Value)
            return Results.NotFound();

        // Verify the caller can assign this role
        var currentRoleStr = GetRoleClaimValue(httpContext);
        if (!Trap_Intel.Domain.Roles.SystemRoles.TryResolveRoleId(currentRoleStr, out var callerRoleId))
            return Results.Forbid();

        if (!RolePermissionMap.CanAssignRole(callerRoleId, newRoleId))
        {
            return Results.Problem(
                title: "Forbidden",
                detail: $"Your role cannot assign the requested role.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        // Sole admin protection
        if (targetUser.RoleId == Trap_Intel.Domain.Roles.SystemRoles.OrganizationAdminId && newRoleId != Trap_Intel.Domain.Roles.SystemRoles.OrganizationAdminId)
        {
            var adminCount = await userRepository.CountAdminsByOrganizationAsync(orgId.Value, cancellationToken);
            if (adminCount <= 1)
            {
                return Results.Problem(
                    title: "Cannot Change Role",
                    detail: "Cannot change the role of the only organization admin.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        var result = targetUser.ChangeRole(newRoleId);
        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Role Change Failed",
                detail: result.Errors.FirstOrDefault()?.Message ?? "Failed to change role.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await userRepository.UpdateAsync(targetUser, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User {TargetUserId} role changed to {NewRole} by {CurrentUserId}",
            userId, newRoleId, currentUserId);

        return Results.Ok(new { message = $"User role changed successfully." });
    }

    private static async Task<IResult> DeactivateUser(
        Guid userId,
        DeactivateUserRequest request,
        IUserRepository userRepository,
        Domain.Abstractions.IUnitOfWork unitOfWork,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetUserId(httpContext);
        var orgId = GetOrganizationId(httpContext);
        if (currentUserId is null || orgId is null)
            return Results.Unauthorized();

        if (currentUserId == userId)
        {
            return Results.Problem(
                title: "Invalid Operation",
                detail: "You cannot deactivate your own account.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var targetUser = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (targetUser is null || targetUser.OrganizationId != orgId.Value)
            return Results.NotFound();

        var result = targetUser.Deactivate(request.Reason);
        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Deactivation Failed",
                detail: result.Errors.FirstOrDefault()?.Message ?? "Failed to deactivate user.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await userRepository.UpdateAsync(targetUser, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deactivated by {AdminId}", userId, currentUserId);
        return Results.Ok(new { message = "User deactivated successfully." });
    }

    private static async Task<IResult> ActivateUser(
        Guid userId,
        IUserRepository userRepository,
        Domain.Abstractions.IUnitOfWork unitOfWork,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetUserId(httpContext);
        var orgId = GetOrganizationId(httpContext);
        if (currentUserId is null || orgId is null)
            return Results.Unauthorized();

        var targetUser = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (targetUser is null || targetUser.OrganizationId != orgId.Value)
            return Results.NotFound();

        var result = targetUser.Activate();
        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Activation Failed",
                detail: result.Errors.FirstOrDefault()?.Message ?? "Failed to activate user.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await userRepository.UpdateAsync(targetUser, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} activated by {AdminId}", userId, currentUserId);
        return Results.Ok(new { message = "User activated successfully." });
    }

    private static async Task<IResult> UnlockUser(
        Guid userId,
        IUserRepository userRepository,
        Domain.Abstractions.IUnitOfWork unitOfWork,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetUserId(httpContext);
        var orgId = GetOrganizationId(httpContext);
        if (currentUserId is null || orgId is null)
            return Results.Unauthorized();

        var targetUser = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (targetUser is null || targetUser.OrganizationId != orgId.Value)
            return Results.NotFound();

        if (!targetUser.IsLockedOut)
        {
            return Results.Problem(
                title: "Not Locked",
                detail: "This user account is not locked.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        targetUser.UnlockAccount();

        await userRepository.UpdateAsync(targetUser, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} unlocked by {AdminId}", userId, currentUserId);
        return Results.Ok(new { message = "User account unlocked successfully." });
    }

    private static async Task<IResult> SuspendUser(
        Guid userId,
        [FromBody] SuspendUserRequest request,
        IUserRepository userRepository,
        HttpContext httpContext,
        ISender sender,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetUserId(httpContext);
        var orgId = GetOrganizationId(httpContext);
        if (currentUserId is null || orgId is null)
            return Results.Unauthorized();

        if (currentUserId == userId)
        {
            return Results.Problem(
                title: "Invalid Operation",
                detail: "You cannot suspend your own account.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var targetUser = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (targetUser is null || targetUser.OrganizationId != orgId.Value)
            return Results.NotFound();

        var command = new SuspendUserCommand(userId, request.Reason ?? "Administrative suspension");
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Code == "Identity.UserNotFound"))
                return Results.NotFound(new { message = result.Errors.First().Message });

            return Results.Problem(
                title: "Failed to suspend user",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        logger.LogInformation("User {UserId} suspended by {AdminId}", userId, currentUserId);
        return Results.Ok(new { message = "User suspended successfully." });
    }

    private static async Task<IResult> UnsuspendUser(
        Guid userId,
        IUserRepository userRepository,
        HttpContext httpContext,
        ISender sender,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetUserId(httpContext);
        var orgId = GetOrganizationId(httpContext);
        if (currentUserId is null || orgId is null)
            return Results.Unauthorized();

        var targetUser = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (targetUser is null || targetUser.OrganizationId != orgId.Value)
            return Results.NotFound();

        var command = new UnsuspendUserCommand(userId);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Code == "Identity.UserNotFound"))
                return Results.NotFound(new { message = result.Errors.First().Message });

            return Results.Problem(
                title: "Failed to unsuspend user",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        logger.LogInformation("User {UserId} unsuspended by {AdminId}", userId, currentUserId);
        return Results.Ok(new { message = "User unsuspended successfully." });
    }

    private static IResult GetMyPermissions(HttpContext httpContext)
    {
        var roleClaim = GetRoleClaimValue(httpContext);
        Guid? roleId = Trap_Intel.Domain.Roles.SystemRoles.TryResolveRoleId(roleClaim, out var parsedRoleId)
            ? parsedRoleId
            : null;
        var roleName = roleId.HasValue
            ? Trap_Intel.Domain.Roles.SystemRoles.GetName(roleId.Value)
            : roleClaim ?? "Unknown";

        var permissions = httpContext.User
            .FindAll("permission")
            .Select(c => c.Value)
            .ToList();

        return Results.Ok(new UserPermissionsResponse
        {
            Role = roleName,
            RoleId = roleId,
            Permissions = permissions
        });
    }

    private static IResult GetAllRolePermissions()
    {
        var roles = new[] { 
            Trap_Intel.Domain.Roles.SystemRoles.SuperAdminId, 
            Trap_Intel.Domain.Roles.SystemRoles.OrganizationAdminId,
            Trap_Intel.Domain.Roles.SystemRoles.SecurityAnalystId,
            Trap_Intel.Domain.Roles.SystemRoles.OperationsAnalystId,
            Trap_Intel.Domain.Roles.SystemRoles.ViewerId,
            Trap_Intel.Domain.Roles.SystemRoles.GuestId 
        };
        var matrix = new Dictionary<string, IReadOnlyList<string>>();

        foreach (var roleId in roles)
        {
            matrix[Trap_Intel.Domain.Roles.SystemRoles.GetName(roleId)] = RolePermissionMap.GetPermissions(roleId);
        }

        return Results.Ok(new RolePermissionsMatrixResponse
        {
            RolePermissions = matrix,
            AllPermissions = Permissions.GetAll()
        });
    }

    private static async Task<IResult> ProcessOverdueInvoices(
        [FromBody] ProcessOverdueInvoicesRequest? request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ProcessOverdueInvoicesCommand(
            RunAtUtc: request?.RunAtUtc,
            ApplyLateFees: request?.ApplyLateFees ?? true,
            LateFeePercent: request?.LateFeePercent ?? 5m,
            DryRun: request?.DryRun ?? false);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Overdue processing failed",
                detail: result.Errors.FirstOrDefault()?.Message ?? "Failed to process overdue invoices.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GenerateMonthlyInvoices(
        [FromBody] GenerateMonthlyInvoicesRequest? request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new GenerateMonthlyInvoicesCommand(
            RunAtUtc: request?.RunAtUtc,
            DryRun: request?.DryRun ?? false);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Monthly invoice generation failed",
                detail: result.Errors.FirstOrDefault()?.Message ?? "Failed to generate monthly invoices.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result.Value);
    }

    #endregion

    #region Helpers

    private static Guid? GetUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var id) ? id : null;
    }

    private static Guid? GetOrganizationId(HttpContext httpContext)
    {
        var org = httpContext.User.FindFirst("org")?.Value;
        return Guid.TryParse(org, out var id) ? id : null;
    }

    private static string? GetRoleClaimValue(HttpContext httpContext)
    {
        return httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
            ?? httpContext.User.FindFirst("role")?.Value;
    }

    private static async Task<AdminUserInfo> MapToAdminUserInfoAsync(
        User u,
        Trap_Intel.Domain.Roles.IRoleRepository roleRepository,
        CancellationToken cancellationToken)
    {
        var resolvedRoleName = (await roleRepository.GetByIdAsync(u.RoleId, cancellationToken))?.Name
            ?? Trap_Intel.Domain.Roles.SystemRoles.GetName(u.RoleId);

        return new AdminUserInfo
    {
        Id = u.Id,
        Email = u.Email.Value,
        UserName = u.UserName.Value,
        FirstName = u.FirstName.Value,
        LastName = u.LastName.Value,
        FullName = u.FullName,
        RoleId = u.RoleId,
        Role = resolvedRoleName,
        Status = u.Status.ToString(),
        EmailConfirmed = u.EmailConfirmed,
        TwoFactorEnabled = u.TwoFactorEnabled,
        LastLoginAt = u.LastLoginAt,
        CreatedAt = u.CreatedAt
    };
    }

    #endregion
}

#region Admin DTOs

public sealed record AdminUserInfo
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string UserName { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public required Guid RoleId { get; init; }
    public required string Role { get; init; }
    public required string Status { get; init; }
    public required bool EmailConfirmed { get; init; }
    public required bool TwoFactorEnabled { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public sealed record OrganizationUsersResponse
{
    public required IReadOnlyList<AdminUserInfo> Users { get; init; }
    public required int TotalCount { get; init; }
}

public sealed record ChangeRoleRequest
{
    [Required(ErrorMessage = "New role is required")]
    public required string NewRole { get; init; }
}

public sealed record DeactivateUserRequest
{
    [Required(ErrorMessage = "Reason is required")]
    [MinLength(5, ErrorMessage = "Reason must be at least 5 characters")]
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public required string Reason { get; init; }
}

public sealed record SuspendUserRequest
{
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; init; }
}

public sealed record UserPermissionsResponse
{
    public required string Role { get; init; }
    public Guid? RoleId { get; init; }
    public required IReadOnlyList<string> Permissions { get; init; }
}

public sealed record RolePermissionsMatrixResponse
{
    public required Dictionary<string, IReadOnlyList<string>> RolePermissions { get; init; }
    public required IReadOnlyList<string> AllPermissions { get; init; }
}

public sealed record ProcessOverdueInvoicesRequest
{
    public DateTime? RunAtUtc { get; init; }
    public bool ApplyLateFees { get; init; } = true;

    [Range(0, 100, ErrorMessage = "LateFeePercent must be between 0 and 100")]
    public decimal LateFeePercent { get; init; } = 5m;

    public bool DryRun { get; init; }
}

public sealed record GenerateMonthlyInvoicesRequest
{
    public DateTime? RunAtUtc { get; init; }
    public bool DryRun { get; init; }
}

#endregion
