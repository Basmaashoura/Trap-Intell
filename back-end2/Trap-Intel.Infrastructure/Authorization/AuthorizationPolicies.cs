using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Infrastructure.Authorization;

/// <summary>
/// Well-known authorization policy names used throughout the application.
/// </summary>
public static class AuthorizationPolicies
{
    // Role-based policies
    public const string SuperAdminOnly = "SuperAdminOnly";
    public const string AdminOnly = "AdminOnly";
    public const string AnalystOrAbove = "AnalystOrAbove";
    public const string ViewerOrAbove = "ViewerOrAbove";

    // Organization isolation
    public const string SameOrganization = "SameOrganization";

    // Permission-based policy prefix (used by PermissionPolicyProvider)
    public const string PermissionPrefix = "Permission:";

    /// <summary>
    /// Builds a policy name for a single permission.
    /// </summary>
    public static string ForPermission(string permission) => $"{PermissionPrefix}{permission}";
}

/// <summary>
/// Dynamic policy provider that creates permission-based policies on demand.
/// Handles policies with the "Permission:" prefix by creating PermissionRequirement.
/// This avoids registering every single permission as a named policy at startup.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Handle dynamic permission policies
        if (policyName.StartsWith(AuthorizationPolicies.PermissionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[AuthorizationPolicies.PermissionPrefix.Length..];

            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Fallback to registered policies
        return _fallbackProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackProvider.GetFallbackPolicyAsync();
    }
}
