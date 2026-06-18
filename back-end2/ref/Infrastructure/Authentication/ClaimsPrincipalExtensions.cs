using System.Security.Claims;
using Infrastructure.Identity;

namespace Infrastructure.Authentication;

/// <summary>
/// Extension methods for ClaimsPrincipal to extract user information from JWT claims.
/// </summary>
internal static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the domain user ID from claims.
    /// Tries multiple claim types for compatibility.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user ID.</returns>
    /// <exception cref="ApplicationException">Thrown when user ID cannot be found.</exception>
    public static Guid GetUserId(this ClaimsPrincipal? principal)
    {
        Guid? userId = principal.TryGetUserId();

        return userId ?? throw new ApplicationException("User ID is unavailable in claims");
    }

    /// <summary>
    /// Tries to get the domain user ID from claims.
    /// Returns null if not found instead of throwing.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user ID or null if not found.</returns>
    public static Guid? TryGetUserId(this ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        // Try custom claim first (preferred)
        string? userIdClaim = principal.FindFirstValue(CustomClaimTypes.DomainUserId);

        // Fall back to standard NameIdentifier
        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // Fall back to sub claim
        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = principal.FindFirstValue("sub");
        }

        return Guid.TryParse(userIdClaim, out Guid userId) ? userId : null;
    }

    /// <summary>
    /// Gets the identity user ID from claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The identity user ID or null if not found.</returns>
    public static Guid? GetIdentityUserId(this ClaimsPrincipal? principal)
    {
        string? claim = principal?.FindFirstValue(CustomClaimTypes.IdentityUserId);
        return Guid.TryParse(claim, out Guid userId) ? userId : null;
    }

    /// <summary>
    /// Gets the user's email from claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The email or empty string if not found.</returns>
    public static string GetEmail(this ClaimsPrincipal? principal)
    {
        return principal?.FindFirstValue(ClaimTypes.Email)
               ?? principal?.FindFirstValue("email")
               ?? string.Empty;
    }

    /// <summary>
    /// Gets all roles assigned to the user.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>List of role names.</returns>
    public static IReadOnlyList<string> GetRoles(this ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return [];
        }

        return principal
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
    }

    /// <summary>
    /// Gets all permissions assigned to the user.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>Set of permission codes.</returns>
    public static IReadOnlySet<string> GetPermissions(this ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return new HashSet<string>();
        }

        return principal
            .FindAll(CustomClaimTypes.Permissions)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the user has a specific permission in their claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="permission">The permission code to check.</param>
    /// <returns>True if the user has the permission.</returns>
    public static bool HasPermission(this ClaimsPrincipal? principal, string permission)
    {
        if (principal is null || string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        return principal
            .FindAll(CustomClaimTypes.Permissions)
            .Any(c => string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the user can view sensitive data.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>True if the user can view sensitive data.</returns>
    public static bool CanViewSensitiveData(this ClaimsPrincipal? principal)
    {
        string? claim = principal?.FindFirstValue(CustomClaimTypes.CanViewSensitiveData);
        return bool.TryParse(claim, out bool canView) && canView;
    }

    /// <summary>
    /// Checks if the user is a Super Admin.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>True if the user is a Super Admin.</returns>
    public static bool IsSuperAdmin(this ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return false;
        }

        return principal.IsInRole("Super Admin") || principal.IsInRole("SuperAdmin");
    }
}
