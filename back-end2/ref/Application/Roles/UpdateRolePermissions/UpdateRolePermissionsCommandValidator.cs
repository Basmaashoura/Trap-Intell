using FluentValidation;

namespace Application.Roles.UpdateRolePermissions;

/// <summary>
/// Validator for UpdateRolePermissionsCommand.
/// </summary>
internal sealed class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage("Role ID is required");

        RuleFor(x => x.PermissionIds)
            .NotNull()
            .WithMessage("Permission IDs list is required");

        RuleForEach(x => x.PermissionIds)
            .NotEmpty()
            .WithMessage("Permission ID cannot be empty");
    }
}
