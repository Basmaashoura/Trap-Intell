using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Identity;

/// <summary>
/// JWT token generator implementation.
/// </summary>
internal sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings)
    {
        _settings = jwtSettings.Value;
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    public TokenResponse GenerateTokens(
        Guid domainUserId,
        Guid identityUserId,
        string email,
        IReadOnlyList<string> roles,
        IReadOnlyList<string>? permissions = null,
        bool canViewSensitiveData = false,
        string? securityStamp = null)
    {
        DateTime now = DateTime.UtcNow;
        DateTime accessTokenExpires = now.AddMinutes(_settings.AccessTokenExpirationMinutes);
        DateTime refreshTokenExpires = now.AddDays(_settings.RefreshTokenExpirationDays);

        string accessToken = GenerateAccessToken(
            domainUserId,
            identityUserId,
            email,
            roles,
            permissions,
            canViewSensitiveData,
            securityStamp,
            accessTokenExpires);

        string refreshToken = GenerateRefreshToken();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessTokenExpires,
            RefreshTokenExpiresAt = refreshTokenExpires
        };
    }

    public TokenValidationResult ValidateAccessToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            ClaimsPrincipal principal = tokenHandler.ValidateToken(token, _validationParameters, out SecurityToken validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return TokenValidationResult.Failed("Invalid token algorithm");
            }

            string? domainUserIdClaim = principal.FindFirstValue(CustomClaimTypes.DomainUserId);
            string? identityUserIdClaim = principal.FindFirstValue(CustomClaimTypes.IdentityUserId);
            string? emailClaim = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue(JwtRegisteredClaimNames.Email);

            if (!Guid.TryParse(domainUserIdClaim, out Guid domainUserId) ||
                !Guid.TryParse(identityUserIdClaim, out Guid identityUserId))
            {
                return TokenValidationResult.Failed("Invalid user claims");
            }

            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            return TokenValidationResult.Success(domainUserId, identityUserId, emailClaim ?? string.Empty, roles);
        }
        catch (SecurityTokenExpiredException)
        {
            return TokenValidationResult.Failed("Token has expired");
        }
        catch (Exception ex)
        {
            return TokenValidationResult.Failed($"Token validation failed: {ex.Message}");
        }
    }

    private string GenerateAccessToken(
        Guid domainUserId,
        Guid identityUserId,
        string email,
        IReadOnlyList<string> roles,
        IReadOnlyList<string>? permissions,
        bool canViewSensitiveData,
        string? securityStamp,
        DateTime expires)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, domainUserId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new(CustomClaimTypes.DomainUserId, domainUserId.ToString()),
            new(CustomClaimTypes.IdentityUserId, identityUserId.ToString()),
            new(CustomClaimTypes.CanViewSensitiveData, canViewSensitiveData.ToString().ToUpperInvariant())
        };

        // Add security stamp for instant token invalidation
        if (!string.IsNullOrEmpty(securityStamp))
        {
            claims.Add(new Claim(CustomClaimTypes.SecurityStamp, securityStamp));
        }

        // Add role claims
        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add permission claims for fast authorization checks
        if (permissions is not null)
        {
            foreach (string permission in permissions)
            {
                claims.Add(new Claim(CustomClaimTypes.Permissions, permission));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        byte[] randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}

/// <summary>
/// Custom claim types for JWT tokens.
/// </summary>
public static class CustomClaimTypes
{
    public const string DomainUserId = "domain_user_id";
    public const string IdentityUserId = "identity_user_id";
    public const string Permissions = "permissions";
    public const string CanViewSensitiveData = "can_view_sensitive_data";
    public const string SecurityStamp = "security_stamp";
}
