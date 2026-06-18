using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Infrastructure.Authentication.Configuration;

namespace Trap_Intel.Infrastructure.Authentication.Services;

/// <summary>
/// Service for generating and validating JWT access tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generate a JWT access token for a user.
    /// </summary>
    string GenerateAccessToken(User user, IEnumerable<string> permissions);

    /// <summary>
    /// Validate a JWT access token and return claims principal.
    /// </summary>
    ClaimsPrincipal? ValidateAccessToken(string token);

    /// <summary>
    /// Get user ID from token without full validation (for refresh flow).
    /// </summary>
    Guid? GetUserIdFromExpiredToken(string token);

    /// <summary>
    /// Get the token validation parameters for middleware configuration.
    /// </summary>
    TokenValidationParameters GetTokenValidationParameters();
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly TokenValidationParameters _validationParameters;
    private readonly SigningCredentials _signingCredentials;
    private readonly SymmetricSecurityKey _securityKey;
    private readonly ILogger<JwtTokenService>? _logger;

    public JwtTokenService(IOptions<JwtSettings> settings, ILogger<JwtTokenService>? logger = null)
    {
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;

        // Validate secret key length
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is not configured");
        }

        if (_settings.SecretKey.Length < JwtSettings.MinimumSecretKeyLength)
        {
            throw new InvalidOperationException(
                $"JWT SecretKey must be at least {JwtSettings.MinimumSecretKeyLength} characters");
        }

        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        _signingCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _securityKey,
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero, // No tolerance for expiration
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };
    }

    public string GenerateAccessToken(User user, IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_settings.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("org", user.OrganizationId.ToString()),
            new(ClaimTypes.Role, user.RoleId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new("name", user.FullName),
            new("security_stamp", user.SecurityStamp ?? string.Empty)
        };

        // Add permissions as claims (for authorization)
        if (permissions != null)
        {
            foreach (var permission in permissions.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                claims.Add(new Claim("permission", permission));
            }
        }

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: _signingCredentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger?.LogDebug(
            "Generated access token for user {UserId}, expires at {ExpiresAt}",
            user.Id, expires);

        return tokenString;
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _validationParameters, out var validatedToken);

            // Additional validation: ensure it's a JWT
            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                _logger?.LogWarning("Token validation failed: not a valid JWT");
                return null;
            }

            // Validate algorithm to prevent algorithm confusion attacks
            if (!jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger?.LogWarning("Token validation failed: invalid algorithm {Algorithm}", jwtToken.Header.Alg);
                return null;
            }

            return principal;
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger?.LogDebug(ex, "Token has expired");
            return null;
        }
        catch (SecurityTokenException ex)
        {
            _logger?.LogWarning(ex, "Token validation failed");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error during token validation");
            return null;
        }
    }

    public Guid? GetUserIdFromExpiredToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();

            // Validate without checking expiration (for refresh flow)
            var paramsWithoutLifetime = _validationParameters.Clone();
            paramsWithoutLifetime.ValidateLifetime = false;

            var principal = handler.ValidateToken(token, paramsWithoutLifetime, out var validatedToken);

            // Validate algorithm
            if (validatedToken is JwtSecurityToken jwtToken &&
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger?.LogWarning("Token has invalid algorithm during refresh: {Algorithm}", jwtToken.Header.Alg);
                return null;
            }

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            _logger?.LogWarning("Could not extract user ID from expired token");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to extract user ID from expired token");
            return null;
        }
    }

    public TokenValidationParameters GetTokenValidationParameters()
    {
        return _validationParameters.Clone();
    }
}
