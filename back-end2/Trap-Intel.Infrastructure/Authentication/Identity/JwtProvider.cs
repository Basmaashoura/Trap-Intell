using Microsoft.Extensions.Options;
using Trap_Intel.Application.Abstractions.Identity;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Roles;
using Trap_Intel.Infrastructure.Authentication.Configuration;
using Trap_Intel.Infrastructure.Authentication.Services;

namespace Trap_Intel.Infrastructure.Authentication.Identity;

internal sealed class JwtProvider : IJwtProvider
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IRoleRepository _roleRepository;
    private readonly JwtSettings _jwtSettings;

    public JwtProvider(
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IAuthenticationService authenticationService,
        IRoleRepository roleRepository,
        IOptions<JwtSettings> jwtSettings)
    {
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _authenticationService = authenticationService;
        _roleRepository = roleRepository;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthTokenDto> GenerateAuthTokensAsync(
        User user, 
        string? ipAddress, 
        string? userAgent, 
        bool rememberMe, 
        CancellationToken cancellationToken = default)
    {
        // 1. Get User Permissions from Domain
        var permissions = await ResolvePermissionsAsync(user, cancellationToken);

        // 2. Generate Access Token
        var accessToken = _jwtTokenService.GenerateAccessToken(user, permissions);

        // 3. Generate Refresh Token
        var refreshTokenResult = await _refreshTokenService.CreateTokenAsync(
            user.Id,
            rememberMe,
            ipAddress,
            userAgent,
            cancellationToken);

        if (refreshTokenResult.IsFailure)
        {
            // Fallback or throw based on your error handling policy
            throw new InvalidOperationException($"Could not generate refresh token: {refreshTokenResult.Errors.FirstOrDefault()?.Message}");
        }

        var tokenData = refreshTokenResult.Value;

        // 4. Return DTO
        return new AuthTokenDto(
            accessToken,
            _jwtSettings.AccessTokenExpirationMinutes * 60, // Total seconds
            tokenData.RawToken,
            tokenData.ExpiresAt
        );
    }

    public async Task<Result<TwoFactorLoginChallengeDto>> GenerateTwoFactorLoginChallengeAsync(
        User user,
        string? ipAddress,
        string? userAgent,
        bool rememberMe,
        CancellationToken cancellationToken = default)
    {
        var result = await _authenticationService.GenerateTwoFactorLoginTokenAsync(
            user,
            ipAddress,
            userAgent,
            rememberMe,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<TwoFactorLoginChallengeDto>(result.Errors);
        }

        return Result.Success(new TwoFactorLoginChallengeDto(
            result.Value.TwoFactorToken,
            result.Value.TokenExpiry,
            result.Value.UserId));
    }

    private async Task<string[]> ResolvePermissionsAsync(User user, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);

        if (role is not null && role.Permissions.Count > 0)
        {
            return role.Permissions
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return user.GetPermissions().ToArray();
    }
}
