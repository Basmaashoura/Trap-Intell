using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Authorization;

/// <summary>
/// Represents a permission-based authorization requirement.
/// Used with PermissionAuthorizationHandler for fine-grained access control.
/// </summary>
internal sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Creates a new permission requirement.
    /// </summary>
    /// <param name="permission">The permission code required (e.g., "USERS:CREATE").</param>
    /// <exception cref="ArgumentException">Thrown when permission is null or whitespace.</exception>
    public PermissionRequirement(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission cannot be null or empty.", nameof(permission));
        }

        Permission = permission.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// The permission code required for authorization.
    /// Stored in uppercase for consistent comparison.
    /// </summary>
    public string Permission { get; }

    /// <inheritdoc />
    public override string ToString() => $"PermissionRequirement: {Permission}";

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is PermissionRequirement other &&
               string.Equals(Permission, other.Permission, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Permission);
    }
}
