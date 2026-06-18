using SharedKernel;

namespace Domain.Permissions;

public static class RoleErrors
{
    public static Error NotFound(Guid roleId) => Error.NotFound(
        "Roles.NotFound",
        $"The role with the Id = '{roleId}' was not found");

    public static Error NotFoundByName(string name) => Error.NotFound(
        "Roles.NotFoundByName",
        $"The role with the name = '{name}' was not found");

    public static readonly Error NameNotUnique = Error.Conflict(
        "Roles.NameNotUnique",
        "A role with this name already exists");

    public static readonly Error CannotModifySystemRole = Error.Failure(
        "Roles.CannotModifySystemRole",
        "System roles cannot be modified or deleted");

    public static readonly Error CannotDeleteRoleWithUsers = Error.Failure(
        "Roles.CannotDeleteRoleWithUsers",
        "Cannot delete a role that has users assigned to it");

    public static readonly Error PermissionAlreadyAssigned = Error.Conflict(
        "Roles.PermissionAlreadyAssigned",
        "This permission is already assigned to the role");
}
