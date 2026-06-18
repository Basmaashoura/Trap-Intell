using FluentValidation;

namespace Application.Users.SuspendUser;

/// <summary>
/// Validator for SuspendUserCommand.
/// </summary>
internal sealed class SuspendUserCommandValidator : AbstractValidator<SuspendUserCommand>
{
    public SuspendUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Reason must not exceed 500 characters")
            .When(x => x.Reason is not null);
    }
}
