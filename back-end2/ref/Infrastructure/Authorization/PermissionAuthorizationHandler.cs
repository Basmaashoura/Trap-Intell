using System.Security.Claims;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Authorization;

/// <summary>
/// Handles permission-based authorization requirements.
/// Implements a 3-tier security check:
/// 1. JWT Claims (fastest - no DB hit)
/// 2. Super Admin role (full access)
/// 3. Database lookup (fallback with caching)
/// </summary>
internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // SECURITY CHECK 1: Reject unauthenticated users immediately
        if (context.User.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug(
                "Authorization denied: User is not authenticated for permission {Permission}",
                requirement.Permission);
            return; // Don't call Fail() - let other handlers try or return 401
        }

        // SECURITY CHECK 2: Validate requirement
        if (string.IsNullOrWhiteSpace(requirement.Permission))
        {
            _logger.LogWarning("Authorization denied: Empty permission requirement");
            return;
        }

        string permission = requirement.Permission.Trim();

        // STRATEGY 1: Check permissions from JWT claims (fastest - no DB hit)
        if (HasPermissionInClaims(context.User, permission))
        {
            _logger.LogDebug(
                "Authorization granted via JWT claims for permission {Permission}",
                permission);
            context.Succeed(requirement);
            return;
        }

        // STRATEGY 2: Check if user is Super Admin (has all permissions)
        if (IsSuperAdmin(context.User))
        {
            _logger.LogDebug(
                "Authorization granted: Super Admin bypasses permission check for {Permission}",
                permission);
            context.Succeed(requirement);
            return;
        }

        // STRATEGY 3: Fallback to database lookup (for long-lived tokens or permissions not in JWT)
        bool hasPermissionInDb = await HasPermissionInDatabaseAsync(context.User, permission);
        if (hasPermissionInDb)
        {
            _logger.LogDebug(
                "Authorization granted via database lookup for permission {Permission}",
                permission);
            context.Succeed(requirement);
            return;
        }

        // SECURITY: Permission not found - do NOT call context.Fail()
        // This allows other handlers to potentially succeed and prevents
        // revealing information about what permissions exist
        _logger.LogInformation(
            "Authorization denied: User {UserId} does not have permission {Permission}",
            GetUserIdSafe(context.User),
            permission);
    }

    /// <summary>
    /// Checks if the permission exists in JWT claims.
    /// Uses case-insensitive comparison for robustness.
    /// </summary>
    private static bool HasPermissionInClaims(ClaimsPrincipal user, string permission)
    {
        // Get all permission claims
        IEnumerable<Claim> permissionClaims = user.FindAll(CustomClaimTypes.Permissions);

        // Case-insensitive comparison for robustness
        return permissionClaims.Any(claim =>
            string.Equals(claim.Value, permission, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if user has the Super Admin role.
    /// Super Admins have implicit access to all permissions.
    /// </summary>
    private static bool IsSuperAdmin(ClaimsPrincipal user)
    {
        // Check multiple possible formats for role claim
        return user.IsInRole("Super Admin") ||
               user.IsInRole("SuperAdmin") ||
               user.HasClaim(ClaimTypes.Role, "Super Admin") ||
               user.HasClaim(ClaimTypes.Role, "SuperAdmin");
    }

    /// <summary>
    /// Falls back to database to check permissions.
    /// This handles cases where:
    /// - JWT was issued before permissions were granted
    /// - Permissions are not included in JWT claims
    /// - Permission cache needs refresh
    /// </summary>
    private async Task<bool> HasPermissionInDatabaseAsync(ClaimsPrincipal user, string permission)
    {
        Guid? userId = GetUserId(user);
        if (userId is null)
        {
            _logger.LogWarning("Cannot check database permissions: User ID not found in claims");
            return false;
        }

        try
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            PermissionProvider permissionProvider = scope.ServiceProvider
                .GetRequiredService<PermissionProvider>();

            HashSet<string> permissions = await permissionProvider
                .GetForUserIdAsync(userId.Value);

            return permissions.Contains(permission);
        }
        catch (Exception ex)
        {
            // SECURITY: Log error but don't expose details
            _logger.LogError(
                ex,
                "Error checking database permissions for user {UserId}",
                userId.Value);
            return false;
        }
    }

    /// <summary>
    /// Safely extracts user ID from claims.
    /// Checks multiple claim types for compatibility.
    /// </summary>
    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        // Try custom claim first (preferred)
        string? userIdClaim = user.FindFirstValue(CustomClaimTypes.DomainUserId);

        // Fall back to standard claims
        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = user.FindFirstValue("sub");
        }

        return Guid.TryParse(userIdClaim, out Guid userId) ? userId : null;
    }

    /// <summary>
    /// Gets user ID for logging purposes (safe - won't throw).
    /// </summary>
    private static string GetUserIdSafe(ClaimsPrincipal user)
    {
        return GetUserId(user)?.ToString() ?? "unknown";
    }
}
