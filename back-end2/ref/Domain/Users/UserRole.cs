using Domain.Permissions;
using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Junction table for User-Role many-to-many relationship.
/// A user can have multiple roles.
/// </summary>
public sealed class UserRole : Entity
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    /// <summary>
    /// When this role was assigned to the user.
    /// </summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>
    /// Who assigned this role (Admin user ID).
    /// </summary>
    public Guid? AssignedBy { get; private set; }

    /// <summary>
    /// Navigation property to User.
    /// </summary>
    public User? User { get; init; }

    /// <summary>
    /// Navigation property to Role.
    /// </summary>
    public Role? Role { get; init; }

    private UserRole()
    {
    }

    public static UserRole Create(Guid userId, Guid roleId, Guid? assignedBy = null)
    {
        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy
        };

        userRole.Raise(new UserRoleAssignedDomainEvent(userId, roleId));

        return userRole;
    }
}
