using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Roles;

/// <summary>
/// Domain errors relating to user roles.
/// </summary>
public static class RoleErrors
{
    public static readonly Error NameNotUnique = Error.Custom(
        "Role.NameNotUnique",
        "A role with this name already exists in the organization.");

    public static readonly Error InvalidName = Error.Custom(
        "Role.InvalidName",
        "The role name cannot be empty or exceed 100 characters.");

    public static readonly Error CannotModifySystemRole = Error.Custom(
        "Role.CannotModifySystemRole",
        "System defined roles cannot be modified or deleted.");

    public static readonly Error CannotDeleteActiveRole = Error.Custom(
        "Role.CannotDeleteActiveRole",
        "An active role cannot be deleted. Deactivate it first.");

    public static readonly Error DuplicatePermission = Error.Custom(
        "Role.DuplicatePermission",
        "The permission you are trying to add has already been granted.");

    public static readonly Error InvalidPermission = Error.Custom(
        "Role.InvalidPermission",
        "One or more permissions granted to this role do not exist in the system registry.");

    public static readonly Error ScopeViolation = Error.Custom(
        "Role.ScopeViolation",
        "You are not allowed to manage roles outside your organization.");

    public static readonly Error RoleInUse = Error.Custom(
        "Role.RoleInUse",
        "Cannot delete a role that is currently assigned to users.");

    public static readonly Error RoleInactive = Error.Custom(
        "Role.RoleInactive",
        "Cannot modify permissions for an inactive role.");

    public static Error RoleNotFound(Guid roleId) => Error.Custom(
        "Role.NotFound",
        $"Role '{roleId}' could not be found.");
}
