using Application.Abstractions.Identity;

namespace Infrastructure.Identity;

/// <summary>
/// Generates JWT access and refresh tokens.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates access and refresh tokens for a user.
    /// </summary>
    /// <param name="domainUserId">The domain user ID.</param>
    /// <param name="identityUserId">The identity user ID.</param>
    /// <param name="email">The user's email.</param>
    /// <param name="roles">The user's roles.</param>
    /// <param name="permissions">The user's permissions (optional).</param>
    /// <param name="canViewSensitiveData">Whether user can view sensitive data.</param>
    /// <param name="securityStamp">The security stamp for instant token invalidation (optional).</param>
    /// <returns>Token response with access and refresh tokens.</returns>
    TokenResponse GenerateTokens(
        Guid domainUserId,
        Guid identityUserId,
        string email,
        IReadOnlyList<string> roles,
        IReadOnlyList<string>? permissions = null,
        bool canViewSensitiveData = false,
        string? securityStamp = null);

    /// <summary>
    /// Validates an access token and returns claims if valid.
    /// </summary>
    TokenValidationResult ValidateAccessToken(string token);
}

/// <summary>
/// Result of token validation.
/// </summary>
public sealed class TokenValidationResult
{
    public bool IsValid { get; private init; }
    public Guid? DomainUserId { get; private init; }
    public Guid? IdentityUserId { get; private init; }
    public string? Email { get; private init; }
    public IReadOnlyList<string>? Roles { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static TokenValidationResult Success(
        Guid domainUserId,
        Guid identityUserId,
        string email,
        IReadOnlyList<string> roles) => new()
        {
            IsValid = true,
            DomainUserId = domainUserId,
            IdentityUserId = identityUserId,
            Email = email,
            Roles = roles
        };

    public static TokenValidationResult Failed(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}
