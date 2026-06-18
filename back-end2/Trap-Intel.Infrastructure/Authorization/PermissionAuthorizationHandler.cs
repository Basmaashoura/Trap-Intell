using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Roles;
using System.Security.Claims;

namespace Trap_Intel.Infrastructure.Authorization;

/// <summary>
/// Handles PermissionRequirement by checking the "permission" claims in the JWT token.
/// Permissions are embedded in the token at login time from the user's role mapping.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // SuperAdmin role always passes
        var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value
            ?? context.User.FindFirst("role")?.Value;
        var isSuperAdmin = SystemRoles.IsSuperAdmin(roleClaim);

        if (isSuperAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogDebug("Permission check failed: no user identity");
            return Task.CompletedTask;
        }

        // Collect all permission claims from the token
        var userPermissions = context.User
            .FindAll("permission")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        bool hasAccess;

        if (requirement.RequireAll)
        {
            hasAccess = requirement.Permissions.All(p => userPermissions.Contains(p));
        }
        else
        {
            hasAccess = requirement.Permissions.Any(p => userPermissions.Contains(p));
        }

        if (hasAccess)
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Permission denied for user {UserId}. Required: [{Required}], Has: [{Has}]",
                userId,
                string.Join(", ", requirement.Permissions),
                string.Join(", ", userPermissions));
        }

        return Task.CompletedTask;
    }
}
