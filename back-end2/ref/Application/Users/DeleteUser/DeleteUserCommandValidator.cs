using FluentValidation;

namespace Application.Users.DeleteUser;

/// <summary>
/// Validator for DeleteUserCommand.
/// </summary>
internal sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
}
