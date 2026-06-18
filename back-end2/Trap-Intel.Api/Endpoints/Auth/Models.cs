using System.ComponentModel.DataAnnotations;

namespace Trap_Intel.Api.Endpoints.Auth.Models;

public sealed record PasswordValidationRequest
{
    [Required(ErrorMessage = "Password is required")]
    [MinLength(1, ErrorMessage = "Password cannot be empty")]
    [MaxLength(128, ErrorMessage = "Password cannot exceed 128 characters")]
    public required string Password { get; init; }
}

public sealed record PasswordValidationResponse
{
    public required string Message { get; init; }
    public required bool IsValid { get; init; }
}

public sealed record LogoutRequest
{
    public string? RefreshToken { get; init; }
    public bool LogoutAll { get; init; } = false;
}

public sealed record ActiveSessionsResponse
{
    public required int Count { get; init; }
    public required Guid UserId { get; init; }
}

public sealed record ValidateResetTokenResponse
{
    public required bool IsValid { get; init; }
    public required string Message { get; init; }
}

public sealed record ValidateResetTokenRequest
{
    [Required(ErrorMessage = "Token is required")]
    public required string Token { get; init; }
}

public sealed record ResendVerificationRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public required string Email { get; init; }
}

public sealed record ConfirmTwoFactorRequest
{
    [Required(ErrorMessage = "Setup token is required")]
    public required string SetupToken { get; init; }

    [Required(ErrorMessage = "TOTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
    public required string Code { get; init; }
}

public sealed record VerifyTwoFactorRequest
{
    [Required(ErrorMessage = "Two-factor token is required")]
    public required string TwoFactorToken { get; init; }

    [Required(ErrorMessage = "Code is required")]
    public required string Code { get; init; }

    public bool IsBackupCode { get; init; } = false;
    public bool RememberMe { get; init; } = false;
}

public sealed record DisableTwoFactorRequest
{
    [Required(ErrorMessage = "Password is required")]
    public required string Password { get; init; }
}

public sealed record RegenerateBackupCodesRequest
{
    [Required(ErrorMessage = "TOTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
    public required string Code { get; init; }
}

public sealed record TwoFactorSetupResponse
{
    public required string SetupToken { get; init; }
    public required string Secret { get; init; }
    public required string SecretFormatted { get; init; }
    public required string QrCodeData { get; init; }
    public required string OtpAuthUri { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

public sealed record TwoFactorConfirmResponse
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public required IReadOnlyList<string> BackupCodes { get; init; }
}

public sealed record BackupCodesResponse
{
    public required IReadOnlyList<string> BackupCodes { get; init; }
    public required string Message { get; init; }
}

public sealed record TwoFactorStatusResponse
{
    public required bool IsEnabled { get; init; }
    public required int RemainingBackupCodes { get; init; }
}


