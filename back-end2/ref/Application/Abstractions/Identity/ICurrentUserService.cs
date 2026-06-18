namespace Application.Abstractions.Identity;

/// <summary>
/// Provides information about the current authenticated user.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's domain ID.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets the current user's identity ID.
    /// </summary>
    Guid IdentityUserId { get; }

    /// <summary>
    /// Gets the current user's email.
    /// </summary>
    string Email { get; }

    /// <summary>
    /// Gets whether the user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current user's roles.
    /// </summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Gets the current user's permissions.
    /// </summary>
    IReadOnlySet<string> Permissions { get; }

    /// <summary>
    /// Gets whether the user can view sensitive data.
    /// </summary>
    bool CanViewSensitiveData { get; }

    /// <summary>
    /// Gets whether the user is a Super Admin.
    /// </summary>
    bool IsSuperAdmin { get; }

    /// <summary>
    /// Checks if user has a specific role.
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Checks if user has a specific permission.
    /// </summary>
    bool HasPermission(string permission);
}
