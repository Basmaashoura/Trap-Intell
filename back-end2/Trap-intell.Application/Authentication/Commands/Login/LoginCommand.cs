using MediatR;
using FluentValidation;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Application.Abstractions.Identity;
using Trap_Intel.Domain.Identity;

namespace Trap_Intel.Application.Authentication.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    bool RememberMe,
    string? IpAddress,
    string? UserAgent) : IRequest<Result<AuthTokenDto>>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtProvider _jwtProvider;

    public LoginCommandHandler(IIdentityService identityService, IJwtProvider jwtProvider)
    {
        _identityService = identityService;
        _jwtProvider = jwtProvider;
    }

    public async Task<Result<AuthTokenDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate credentials against Identity Service
        var userResult = await _identityService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);

        if (userResult.IsFailure)
        {
            return Result.Failure<AuthTokenDto>(userResult.Errors);
        }

        // 2. If 2FA is enabled, return a challenge token and require verification before issuing auth tokens.
        if (userResult.Value.TwoFactorEnabled)
        {
            var twoFactorChallenge = await _jwtProvider.GenerateTwoFactorLoginChallengeAsync(
                userResult.Value,
                request.IpAddress,
                request.UserAgent,
                request.RememberMe,
                cancellationToken);

            if (twoFactorChallenge.IsFailure)
            {
                return Result.Failure<AuthTokenDto>(twoFactorChallenge.Errors);
            }

            return Result.Failure<AuthTokenDto>(IdentityErrors.TwoFactorRequired.WithData(new Dictionary<string, object>
            {
                ["TwoFactorToken"] = twoFactorChallenge.Value.TwoFactorToken,
                ["TokenExpiry"] = twoFactorChallenge.Value.TokenExpiry,
                ["UserId"] = twoFactorChallenge.Value.UserId
            }));
        }

        // 3. Generate tokens using Jwt Provider abstraction
        var authTokenDto = await _jwtProvider.GenerateAuthTokensAsync(
            userResult.Value, 
            request.IpAddress, 
            request.UserAgent, 
            request.RememberMe, 
            cancellationToken);

        return Result.Success(authTokenDto);
    }
}