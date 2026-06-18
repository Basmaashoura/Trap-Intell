using SharedKernel;

namespace Domain.Permissions;

/// <summary>
/// Represents a specific permission (action on a module).
/// Example: "Read StressData", "Create Users"
/// </summary>
public sealed class Permission : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The system module this permission applies to.
    /// </summary>
    public SystemModule Module { get; private set; }

    /// <summary>
    /// The action allowed by this permission.
    /// </summary>
    public PermissionAction Action { get; private set; }

    /// <summary>
    /// Unique permission code (e.g., "users:create", "stressdata:read").
    /// Used for authorization checks.
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Human-readable name for display.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Description of what this permission allows.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Navigation property for roles that have this permission.
    /// </summary>
    public List<RolePermission> RolePermissions { get; private set; } = [];

    private Permission()
    {
    }

    public static Permission Create(
        SystemModule module,
        PermissionAction action,
        string name,
        string? description = null)
    {
        string code = $"{module.ToString().ToUpperInvariant()}:{action.ToString().ToUpperInvariant()}";

        return new Permission
        {
            Id = Guid.NewGuid(),
            Module = module,
            Action = action,
            Code = code,
            Name = name,
            Description = description
        };
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
    }
}
