using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Infrastructure.Authorization;

/// <summary>
/// Custom authorization policy provider that creates permission-based policies dynamically.
/// This allows using [Authorize(Policy = "USERS:CREATE")] without pre-registering each policy.
/// </summary>
internal sealed class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private readonly AuthorizationOptions _authorizationOptions;

    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
        _authorizationOptions = options.Value;
    }

    /// <summary>
    /// Gets or creates an authorization policy for the given policy name.
    /// If the policy doesn't exist, creates a new permission-based policy.
    /// </summary>
    /// <param name="policyName">The policy name (permission code).</param>
    /// <returns>The authorization policy.</returns>
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // First, check if a policy with this name already exists
        AuthorizationPolicy? existingPolicy = await base.GetPolicyAsync(policyName);
        if (existingPolicy is not null)
        {
            return existingPolicy;
        }

        // Validate the policy name before creating a permission requirement
        if (string.IsNullOrWhiteSpace(policyName))
        {
            return null;
        }

        // Create a new policy with the permission requirement
        // The policy name IS the permission code (e.g., "USERS:CREATE")
        AuthorizationPolicy permissionPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser() // Must be authenticated first
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();

        // Cache the policy for future requests
        _authorizationOptions.AddPolicy(policyName, permissionPolicy);

        return permissionPolicy;
    }
}
