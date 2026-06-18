using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Authorization;

/// <summary>
/// Attribute to require a specific permission for an endpoint or controller.
/// Usage: [HasPermission(Permissions.Users.Create)]
/// </summary>
/// <remarks>
/// This attribute leverages the permission-based authorization system.
/// The permission string becomes the policy name, which is handled by
/// <see cref="PermissionAuthorizationPolicyProvider"/> and <see cref="PermissionAuthorizationHandler"/>.
/// </remarks>
/// <example>
/// <code>
/// // On a minimal API endpoint
/// app.MapPost("/users", CreateUser).RequireAuthorization(Permissions.Users.Create);
/// 
/// // On a controller method
/// [HasPermission(Permissions.Users.Create)]
/// public IActionResult CreateUser() { }
/// 
/// // Multiple permissions (all required)
/// [HasPermission(Permissions.Users.Read)]
/// [HasPermission(Permissions.Users.Update)]
/// public IActionResult UpdateUser() { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Creates a new permission requirement attribute.
    /// </summary>
    /// <param name="permission">
    /// The permission code required (e.g., "USERS:CREATE").
    /// Use constants from <see cref="Permissions"/> class.
    /// </param>
    public HasPermissionAttribute(string permission)
        : base(policy: permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }

    /// <summary>
    /// The permission code this attribute requires.
    /// </summary>
    public string Permission { get; }
}
