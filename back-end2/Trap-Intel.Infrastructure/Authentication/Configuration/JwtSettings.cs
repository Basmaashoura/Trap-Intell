using System.ComponentModel.DataAnnotations;

namespace Trap_Intel.Infrastructure.Authentication.Configuration;

/// <summary>
/// JWT configuration settings.
/// Implements IValidatableObject for startup validation.
/// </summary>
public sealed class JwtSettings : IValidatableObject
{
    public const string SectionName = "Authentication:Jwt";
    public const int MinimumSecretKeyLength = 32; // 256 bits for HS256

    /// <summary>
    /// Secret key for signing tokens. In production, use Azure Key Vault or similar.
    /// Minimum 256 bits (32 characters) for HS256.
    /// </summary>
    [Required(ErrorMessage = "JWT SecretKey is required")]
    [MinLength(MinimumSecretKeyLength, ErrorMessage = "JWT SecretKey must be at least 32 characters")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer (your application identifier).
    /// </summary>
    [Required(ErrorMessage = "JWT Issuer is required")]
    public string Issuer { get; set; } = "trap-intel";

    /// <summary>
    /// Token audience (API identifier).
    /// </summary>
    [Required(ErrorMessage = "JWT Audience is required")]
    public string Audience { get; set; } = "trap-intel-api";

    /// <summary>
    /// Access token lifetime in minutes.
    /// Recommended: 15-30 minutes.
    /// </summary>
    [Range(1, 60, ErrorMessage = "AccessTokenExpirationMinutes must be between 1 and 60")]
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Validates the JWT settings.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            yield return new ValidationResult(
                "JWT SecretKey cannot be empty or whitespace",
                new[] { nameof(SecretKey) });
        }
        else if (SecretKey.Length < MinimumSecretKeyLength)
        {
            yield return new ValidationResult(
                $"JWT SecretKey must be at least {MinimumSecretKeyLength} characters for security",
                new[] { nameof(SecretKey) });
        }

        // Warn if using weak or default keys
        if (SecretKey.Contains("YOUR-") || SecretKey.Contains("secret") || SecretKey.Contains("password"))
        {
            yield return new ValidationResult(
                "JWT SecretKey appears to be a placeholder or weak key. Use a strong, random key in production.",
                new[] { nameof(SecretKey) });
        }
    }
}
