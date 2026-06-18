using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;

namespace Trap_Intel.Application.Abstractions.Identity;

public sealed record AuthTokenDto(string AccessToken, int ExpiresIn, string RefreshToken, DateTime RefreshTokenExpiresAt);
public sealed record TwoFactorLoginChallengeDto(string TwoFactorToken, DateTime TokenExpiry, Guid UserId);

public interface IJwtProvider
{
    Task<AuthTokenDto> GenerateAuthTokensAsync(User user, string? ipAddress, string? userAgent, bool rememberMe, CancellationToken cancellationToken = default);
    Task<Result<TwoFactorLoginChallengeDto>> GenerateTwoFactorLoginChallengeAsync(User user, string? ipAddress, string? userAgent, bool rememberMe, CancellationToken cancellationToken = default);
}
