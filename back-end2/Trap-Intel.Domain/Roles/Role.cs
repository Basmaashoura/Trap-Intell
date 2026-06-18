using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity.Policies;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Trap_Intel.Domain.Roles;

/// <summary>
/// Domain model for an assignable role with dynamic permissions.
/// Supports both System-wide roles and Tenant-specific custom roles.
/// </summary>
public sealed class Role : Entity<Guid>
{
    private readonly HashSet<string> _permissions = new();

    private Role() : base(Guid.Empty) { } // EF Core

    private Role(
        Guid id,
        string name,
        string description,
        Guid? organizationId,
        bool isSystemRole) : base(id)
    {
        Name = name;
        Description = description;
        OrganizationId = organizationId;
        IsSystemRole = isSystemRole;
        IsActive = true;
    }

    /// <summary>
    /// The display name of the role (e.g. Incident Responder).
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Detailed description of the role's purpose.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// The tenant the custom role belongs to. 
    /// If Null, it is a global System role (like SuperAdmin, SecurityAnalyst).
    /// </summary>
    public Guid? OrganizationId { get; private set; }

    /// <summary>
    /// Indicates whether a role was created by the platform natively. 
    /// System roles cannot be renamed or deleted by tenants.
    /// </summary>
    public bool IsSystemRole { get; private set; }

    /// <summary>
    /// If false, the role cannot be actively assigned to users.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Indicates if the role is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Date when the role was soft-deleted.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Provides read-only access to the permission strings.
    /// </summary>
    public IReadOnlyCollection<string> Permissions => _permissions.ToList().AsReadOnly();

    #region Factory Methods

    /// <summary>
    /// Creates a new tenant-specific custom role.
    /// </summary>
    public static Result<Role> CreateCustomRole(Guid organizationId, string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            return Result.Failure<Role>(RoleErrors.InvalidName);

        var role = new Role(
            Guid.NewGuid(),
            name.Trim(),
            description.Trim(),
            organizationId,
            isSystemRole: false);

        role.RaiseDomainEvent(new RoleCreatedEvent(role.Id, role.OrganizationId, role.Name, DateTime.UtcNow));

        return Result.Success(role);
    }

    /// <summary>
    /// Native setup builder for predefined system roles.
    /// </summary>
    public static Role CreateSystemRole(Guid id, string name, string description, IEnumerable<string> startingPermissions)
    {
        var role = new Role(id, name, description, null, isSystemRole: true);

        foreach(var permit in startingPermissions)
            role._permissions.Add(permit);

        return role;
    }

    #endregion

    #region Domain Actions

    /// <summary>
    /// Validates and applies a full replacement of the role's permissions.
    /// </summary>
    public Result UpdatePermissions(IEnumerable<string> newPermissions)
    {
        if (IsSystemRole)
            return Result.Failure(RoleErrors.CannotModifySystemRole);

        // Compute deltas to fire events
        var newSet = new HashSet<string>(newPermissions);
        var added = newSet.Except(_permissions).ToList();
        var removed = _permissions.Except(newSet).ToList();

        if (!added.Any() && !removed.Any())
            return Result.Success(); // No changes

        // Hard business validation: A custom role cannot escalate to have system-managing powers.
        if (added.Any(p => p.StartsWith("system:")))
            return Result.Failure(RoleErrors.InvalidPermission);

        _permissions.Clear();
        foreach (var p in newSet)
            _permissions.Add(p);

        RaiseDomainEvent(new RolePermissionsUpdatedEvent(
            Id,
            OrganizationId,
            added,
            removed,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Renames a custom role.
    /// </summary>
    public Result UpdateDetails(string name, string description)
    {
        if (IsSystemRole)
            return Result.Failure(RoleErrors.CannotModifySystemRole);

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            return Result.Failure(RoleErrors.InvalidName);

        Name = name.Trim();
        Description = description.Trim();

        return Result.Success();
    }

    /// <summary>
    /// Deactivates a role, stopping new assignments but maintaining existing ones (optionally).
    /// </summary>
    public Result Deactivate()
    {
        if (IsSystemRole)
            return Result.Failure(RoleErrors.CannotModifySystemRole);

        if (!IsActive)
            return Result.Success();

        IsActive = false;
        RaiseDomainEvent(new RoleDeactivatedEvent(Id, OrganizationId, DateTime.UtcNow));

        return Result.Success();
    }

    public Result Activate()
    {
        if (IsSystemRole)
            return Result.Failure(RoleErrors.CannotModifySystemRole);

        if (IsActive)
            return Result.Success();

        IsActive = true;
        RaiseDomainEvent(new RoleActivatedEvent(Id, OrganizationId, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Soft deletes the custom role. Must be deactivated first.
    /// </summary>
    public Result Delete()
    {
        if (IsSystemRole)
            return Result.Failure(RoleErrors.CannotModifySystemRole);

        if (IsActive)
            return Result.Failure(RoleErrors.CannotDeleteActiveRole);

        if (IsDeleted)
            return Result.Success();

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new RoleDeletedEvent(Id, OrganizationId, DateTime.UtcNow));

        return Result.Success();
    }

    #endregion

    #region Reconstruct

    /// <summary>
    /// Infrastructure utility to reload role from DB without firing events.
    /// </summary>
    public static Role Reconstruct(
        Guid id, 
        string name, 
        string description, 
        Guid? organizationId, 
        bool isSystemRole, 
        bool isActive, 
        bool isDeleted, 
        DateTime? deletedAt, 
        IEnumerable<string> permissions)
    {
        var role = new Role
        {
            Id = id,
            Name = name,
            Description = description,
            OrganizationId = organizationId,
            IsSystemRole = isSystemRole,
            IsActive = isActive,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

        foreach (var p in permissions)
            role._permissions.Add(p);

        return role;
    }

    #endregion
}
