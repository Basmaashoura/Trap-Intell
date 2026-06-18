using Microsoft.AspNetCore.Authorization;

namespace Trap_Intel.Infrastructure.Authorization;

/// <summary>
/// Authorization requirement that demands one or more permissions.
/// Used with PermissionAuthorizationHandler for permission-based access control.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// The permissions required (user must have at least one).
    /// </summary>
    public IReadOnlyList<string> Permissions { get; }

    /// <summary>
    /// If true, user must have ALL permissions. If false, ANY one suffices.
    /// </summary>
    public bool RequireAll { get; }

    public PermissionRequirement(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        Permissions = [permission];
        RequireAll = false;
    }

    public PermissionRequirement(IEnumerable<string> permissions, bool requireAll = false)
    {
        var list = permissions.ToList();
        if (list.Count == 0)
            throw new ArgumentException("At least one permission is required.", nameof(permissions));

        Permissions = list;
        RequireAll = requireAll;
    }
}

/// <summary>
/// Authorization requirement that demands a minimum role level.
/// </summary>
public sealed class RoleHierarchyRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// The minimum role name required.
    /// </summary>
    public Guid MinimumRole { get; }

    public RoleHierarchyRequirement(Guid minimumRole)
    {
        MinimumRole = minimumRole;
    }
}

/// <summary>
/// Authorization requirement that ensures the user belongs to the same organization
/// as the resource being accessed.
/// </summary>
public sealed class SameOrganizationRequirement : IAuthorizationRequirement;
