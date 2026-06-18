using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Domain.Roles;
using System.Security.Claims;
using System;

namespace Trap_Intel.Infrastructure.Authorization;

/// <summary>
/// Handles RoleHierarchyRequirement by comparing the user's role level
/// against the minimum required role level.
/// </summary>
public sealed class RoleHierarchyAuthorizationHandler : AuthorizationHandler<RoleHierarchyRequirement>
{
    private readonly ILogger<RoleHierarchyAuthorizationHandler> _logger;

    public RoleHierarchyAuthorizationHandler(ILogger<RoleHierarchyAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleHierarchyRequirement requirement)
    {
        var roleClaim = GetRoleClaimValue(context.User);

        if (string.IsNullOrEmpty(roleClaim))
        {
            _logger.LogDebug("Role hierarchy check failed: no role claim");
            return Task.CompletedTask;
        }

        if (!SystemRoles.TryResolveRoleId(roleClaim, out var userRoleId))
        {
            _logger.LogWarning("Role hierarchy check failed: invalid role claim '{Role}'", roleClaim);
            return Task.CompletedTask;
        }

        var userLevel = RolePermissionMap.GetRoleHierarchy(userRoleId);
        var minLevel = RolePermissionMap.GetRoleHierarchy(requirement.MinimumRole);

        if (userLevel >= minLevel)
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Role hierarchy denied for user role {UserRole} (level {UserLevel}). Minimum: {MinRole} (level {MinLevel})",
                userRoleId, userLevel, requirement.MinimumRole, minLevel);
        }

        return Task.CompletedTask;
    }

    private static string? GetRoleClaimValue(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value
            ?? user.FindFirst("role")?.Value;
    }
}
