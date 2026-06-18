using MediatR;
using FluentValidation;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity.Policies;

namespace Trap_Intel.Application.Authentication.Queries.ValidatePassword;

public sealed record ValidatePasswordQuery(string Password) : IRequest<Result<PasswordValidationResultDto>>;

public sealed record PasswordValidationResultDto(bool IsValid, string Message);

public sealed class ValidatePasswordQueryValidator : AbstractValidator<ValidatePasswordQuery>
{
    public ValidatePasswordQueryValidator()
    {
        RuleFor(x => x.Password).NotEmpty();
    }
}

internal sealed class ValidatePasswordQueryHandler : IRequestHandler<ValidatePasswordQuery, Result<PasswordValidationResultDto>>
{
    public Task<Result<PasswordValidationResultDto>> Handle(ValidatePasswordQuery request, CancellationToken cancellationToken)
    {
        // Check against policy
        var policyResult = PasswordPolicy.ValidatePassword(request.Password);
        if (policyResult.IsFailure)
        {
            return Task.FromResult(Result.Failure<PasswordValidationResultDto>(policyResult.Errors));
        }

        // Check against common passwords
        if (PasswordPolicy.IsCommonPassword(request.Password))
        {
            return Task.FromResult(Result.Failure<PasswordValidationResultDto>(
                Error.Custom("Identity.CommonPassword", "This password is too common. Please choose a more secure password.")));
        }

        return Task.FromResult(Result.Success(new PasswordValidationResultDto(true, "Password meets all requirements")));
    }
}