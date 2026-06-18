using SharedKernel;

namespace Domain.Permissions;

/// <summary>
/// Junction table for Role-Permission many-to-many relationship.
/// </summary>
public sealed class RolePermission : Entity
{
    public Guid Id { get; private set; }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    /// <summary>
    /// When this permission was assigned to the role.
    /// </summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>
    /// Navigation property to Role.
    /// </summary>
    public Role? Role { get; init; }

    /// <summary>
    /// Navigation property to Permission.
    /// </summary>
    public Permission? Permission { get; init; }

    private RolePermission()
    {
    }

    public static RolePermission Create(Guid roleId, Guid permissionId)
    {
        return new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId,
            AssignedAt = DateTime.UtcNow
        };
    }
}
