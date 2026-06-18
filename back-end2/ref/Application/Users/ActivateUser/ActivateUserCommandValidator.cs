using FluentValidation;

namespace Application.Users.ActivateUser;

/// <summary>
/// Validator for ActivateUserCommand.
/// </summary>
internal sealed class ActivateUserCommandValidator : AbstractValidator<ActivateUserCommand>
{
    public ActivateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
}
