using FluentValidation;

namespace Application.Users.Enable2FA;

/// <summary>
/// Validator for Enable2FACommand.
/// </summary>
internal sealed class Enable2FACommandValidator : AbstractValidator<Enable2FACommand>
{
    public Enable2FACommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
}
