using Domain.Users;
using SharedKernel;

namespace Domain.Permissions;

/// <summary>
/// Represents a role (job template) with a set of permissions.
/// Example: "Senior Auditor", "Project Manager", "Developer"
/// </summary>
public sealed class Role : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Role name (e.g., "Senior Auditor", "Super Admin").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Description of the role's responsibilities.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Whether this is a system role that cannot be deleted or modified.
    /// Example: Super Admin role.
    /// </summary>
    public bool IsSystemRole { get; private set; }

    /// <summary>
    /// Whether users with this role can view sensitive data.
    /// If false, sensitive fields (Phone, Email, Revenue) are masked.
    /// </summary>
    public bool CanViewSensitiveData { get; private set; }

    /// <summary>
    /// When the role was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the role was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Whether this role is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Navigation property for permissions assigned to this role.
    /// </summary>
    public List<RolePermission> RolePermissions { get; private set; } = [];

    /// <summary>
    /// Navigation property for users assigned to this role.
    /// </summary>
    public List<UserRole> UserRoles { get; private set; } = [];

    /// <summary>
    /// Navigation property for data masking policy.
    /// </summary>
    public DataMaskingPolicy? MaskingPolicy { get; init; }

    private Role()
    {
    }

    public static Role Create(
        string name,
        string? description = null,
        bool canViewSensitiveData = false,
        bool isSystemRole = false)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CanViewSensitiveData = canViewSensitiveData,
            IsSystemRole = isSystemRole,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        role.Raise(new RoleCreatedDomainEvent(role.Id));

        return role;
    }

    /// <summary>
    /// Creates the default Super Admin role.
    /// </summary>
    public static Role CreateSuperAdmin()
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = "Super Admin",
            Description = "Full system access. Cannot be deleted or modified.",
            CanViewSensitiveData = true,
            IsSystemRole = true,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    /// <summary>
    /// Updates the role properties.
    /// </summary>
    public Result Update(string name, string? description, bool canViewSensitiveData)
    {
        if (IsSystemRole)
        {
            return Result.Failure(RoleErrors.CannotModifySystemRole);
        }

        Name = name;
        Description = description;
        CanViewSensitiveData = canViewSensitiveData;
        UpdatedAt = DateTime.UtcNow;

        Raise(new RoleUpdatedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Deactivates the role.
    /// </summary>
    public Result Deactivate()
    {
        if (IsSystemRole)
        {
            return Result.Failure(RoleErrors.CannotModifySystemRole);
        }

        if (!IsActive)
        {
            return Result.Success(); // Already inactive
        }

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        Raise(new RoleDeactivatedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Activates the role.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the sensitive data access permission.
    /// </summary>
    public Result SetSensitiveDataAccess(bool canView)
    {
        if (IsSystemRole)
        {
            return Result.Failure(RoleErrors.CannotModifySystemRole);
        }

        CanViewSensitiveData = canView;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Checks if this role has a specific permission.
    /// </summary>
    public bool HasPermission(SystemModule module, PermissionAction action)
    {
        return RolePermissions.Exists(rp =>
            rp.Permission != null &&
            rp.Permission.Module == module &&
            rp.Permission.Action == action);
    }

    /// <summary>
    /// Checks if this role has a specific permission by code.
    /// </summary>
    public bool HasPermission(string permissionCode)
    {
        return RolePermissions.Exists(rp =>
            rp.Permission != null &&
            rp.Permission.Code == permissionCode);
    }

    /// <summary>
    /// Gets the count of users with this role.
    /// </summary>
    public int UserCount => UserRoles.Count;
}
