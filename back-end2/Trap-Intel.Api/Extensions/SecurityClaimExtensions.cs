using System.Security.Claims;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Api.Extensions;

internal static class SecurityClaimExtensions
{
    public static string? GetOrganizationClaimValue(this ClaimsPrincipal user)
    {
        return user.FindFirst("org")?.Value
            ?? user.FindFirst("organizationId")?.Value;
    }

    public static string? GetEmailClaimValue(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("email")?.Value;
    }

    public static bool IsSuperAdmin(this ClaimsPrincipal user)
    {
        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value
            ?? user.FindFirst("role")?.Value;

        return SystemRoles.IsSuperAdmin(roleClaim);
    }

    public static bool TryGetOrganizationId(this ClaimsPrincipal user, out Guid organizationId)
    {
        return Guid.TryParse(user.GetOrganizationClaimValue(), out organizationId);
    }

    public static bool IsAuthorizedForOrganization(
        this ClaimsPrincipal user,
        Guid organizationId,
        bool allowSuperAdminBypass = true)
    {
        if (allowSuperAdminBypass && user.IsSuperAdmin())
        {
            return true;
        }

        return user.TryGetOrganizationId(out var claimOrgId) && claimOrgId == organizationId;
    }
}