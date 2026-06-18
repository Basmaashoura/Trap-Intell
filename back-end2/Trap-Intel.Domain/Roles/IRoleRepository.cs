using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trap_Intel.Domain.Roles;

/// <summary>
/// Repository interface for managing dynamic Roles and Permissions.
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Gets a role by its unique ID.
    /// </summary>
    Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by its name within an organization (or system root).
    /// </summary>
    Task<Role?> GetByNameAsync(string name, Guid? organizationId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles for an organization, including inherited system roles.
    /// </summary>
    Task<IReadOnlyList<Role>> GetRolesForOrganizationAsync(Guid organizationId, bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets global system roles across the entire application.
    /// </summary>
    Task<IReadOnlyList<Role>> GetSystemRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a newly created custom role.
    /// </summary>
    Task AddAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role's metadata or permissions.
    /// </summary>
    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a role.
    /// </summary>
    Task DeleteAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role name is unique within an organization.
    /// </summary>
    Task<bool> IsNameUniqueAsync(string name, Guid? organizationId = null, CancellationToken cancellationToken = default);
}
