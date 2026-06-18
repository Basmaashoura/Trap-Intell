using FluentValidation;

namespace Application.Users.DeactivateUser;

/// <summary>
/// Validator for DeactivateUserCommand.
/// </summary>
internal sealed class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
}
