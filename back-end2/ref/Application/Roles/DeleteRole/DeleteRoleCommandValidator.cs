using FluentValidation;

namespace Application.Roles.DeleteRole;

/// <summary>
/// Validator for DeleteRoleCommand.
/// </summary>
internal sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage("Role ID is required");
    }
}
