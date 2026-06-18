using Trap_Intel.Infrastructure.Authorization;

namespace Trap_Intel.Api.Authorization;

/// <summary>
/// Extension methods for applying authorization policies to Minimal API endpoints.
/// Provides a clean, fluent API for permission-based and role-based authorization.
/// </summary>
public static class AuthorizationEndpointExtensions
{
    /// <summary>
    /// Requires the user to have a specific permission.
    /// </summary>
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission)
    {
        return builder.RequireAuthorization(AuthorizationPolicies.ForPermission(permission));
    }

    /// <summary>
    /// Requires the user to have ALL of the specified permissions.
    /// </summary>
    public static RouteHandlerBuilder RequireAllPermissions(this RouteHandlerBuilder builder, params string[] permissions)
    {
        foreach (var permission in permissions)
        {
            builder.RequireAuthorization(AuthorizationPolicies.ForPermission(permission));
        }
        return builder;
    }

    /// <summary>
    /// Requires the user to be a SuperAdmin.
    /// </summary>
    public static RouteHandlerBuilder RequireSuperAdmin(this RouteHandlerBuilder builder)
    {
        return builder.RequireAuthorization(AuthorizationPolicies.SuperAdminOnly);
    }

    /// <summary>
    /// Requires the user to be an admin (OrganizationAdmin or SuperAdmin).
    /// </summary>
    public static RouteHandlerBuilder RequireAdmin(this RouteHandlerBuilder builder)
    {
        return builder.RequireAuthorization(AuthorizationPolicies.AdminOnly);
    }

    /// <summary>
    /// Requires the user to be an analyst or higher.
    /// </summary>
    public static RouteHandlerBuilder RequireAnalystOrAbove(this RouteHandlerBuilder builder)
    {
        return builder.RequireAuthorization(AuthorizationPolicies.AnalystOrAbove);
    }

    /// <summary>
    /// Requires the user to be a viewer or higher (any authenticated non-guest).
    /// </summary>
    public static RouteHandlerBuilder RequireViewerOrAbove(this RouteHandlerBuilder builder)
    {
        return builder.RequireAuthorization(AuthorizationPolicies.ViewerOrAbove);
    }

    /// <summary>
    /// Requires the user to belong to the same organization as the requested resource.
    /// </summary>
    public static RouteHandlerBuilder RequireSameOrganization(this RouteHandlerBuilder builder)
    {
        return builder.RequireAuthorization(AuthorizationPolicies.SameOrganization);
    }
}
