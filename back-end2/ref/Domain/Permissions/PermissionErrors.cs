using SharedKernel;

namespace Domain.Permissions;

public static class PermissionErrors
{
    public static Error NotFound(Guid permissionId) => Error.NotFound(
        "Permissions.NotFound",
        $"The permission with the Id = '{permissionId}' was not found");

    public static Error NotFoundByCode(string code) => Error.NotFound(
        "Permissions.NotFoundByCode",
        $"The permission with the code = '{code}' was not found");

    public static readonly Error CodeNotUnique = Error.Conflict(
        "Permissions.CodeNotUnique",
        "A permission with this code already exists");
}
