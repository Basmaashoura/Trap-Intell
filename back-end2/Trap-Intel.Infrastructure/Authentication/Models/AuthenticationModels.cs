using System.ComponentModel.DataAnnotations;

namespace Trap_Intel.Infrastructure.Authentication.Models;

/// <summary>
/// Response returned after successful authentication.
/// </summary>
public sealed record AuthenticationResponse
{
    /// <summary>
    /// JWT access token for API authentication.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Refresh token for obtaining new access tokens.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Access token expiration time in seconds.
    /// </summary>
    public required int ExpiresIn { get; init; }

    /// <summary>
    /// Refresh token expiration date/time (UTC).
    /// </summary>
    public DateTime? RefreshTokenExpiresAt { get; init; }

    /// <summary>
    /// Token type (always "Bearer").
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// User information.
    /// </summary>
    public required UserInfo User { get; init; }
}

/// <summary>
/// Basic user information returned with authentication response.
/// </summary>
public sealed record UserInfo
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string UserName { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public required Guid RoleId { get; init; }
    public required string Role { get; init; }
    public required Guid OrganizationId { get; init; }
    public required bool EmailConfirmed { get; init; }
    public required bool TwoFactorEnabled { get; init; }
    public required IReadOnlyList<string> Permissions { get; init; }
}

/// <summary>
/// Response when 2FA is required.
/// </summary>
public sealed record TwoFactorRequiredResponse
{
    /// <summary>
    /// Temporary token to use when submitting 2FA code.
    /// </summary>
    public required string TwoFactorToken { get; init; }

    /// <summary>
    /// User ID for reference.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Message indicating 2FA is required.
    /// </summary>
    public string Message { get; init; } = "Two-factor authentication code required.";
}

/// <summary>
/// Request for user login.
/// </summary>
public sealed record LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(1, ErrorMessage = "Password cannot be empty")]
    [MaxLength(128, ErrorMessage = "Password cannot exceed 128 characters")]
    public required string Password { get; init; }

    public bool RememberMe { get; init; } = false;

    [MaxLength(10, ErrorMessage = "2FA code cannot exceed 10 characters")]
    // [RegularExpression(@"^\d{6}$", ErrorMessage = "2FA code must be 6 digits")]
    [RegularExpression(@"^$|^\d{6}$", ErrorMessage = "2FA code must be empty or 6 digits")]
    public string? TwoFactorCode { get; init; }

    public string? TwoFactorToken { get; init; }
}

/// <summary>
/// Request for user registration.
/// </summary>
public sealed record RegisterRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [MaxLength(128, ErrorMessage = "Password cannot exceed 128 characters")]
    public required string Password { get; init; }

    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public required string ConfirmPassword { get; init; }

    [Required(ErrorMessage = "First name is required")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    [RegularExpression(@"^[\p{L}\s\-']+$", ErrorMessage = "First name contains invalid characters")]
    public required string FirstName { get; init; }

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    [RegularExpression(@"^[\p{L}\s\-']+$", ErrorMessage = "Last name contains invalid characters")]
    public required string LastName { get; init; }

    [Required(ErrorMessage = "Username is required")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
    [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_\.\-]+$", ErrorMessage = "Username can only contain letters, numbers, underscores, dots, and hyphens")]
    public required string UserName { get; init; }

    [Required(ErrorMessage = "Invitation token is required")]
    [MinLength(1, ErrorMessage = "Invitation token cannot be empty")]
    [MaxLength(500, ErrorMessage = "Invitation token cannot exceed 500 characters")]
    public required string InvitationToken { get; init; }
}

/// <summary>
/// Request for token refresh.
/// </summary>
public sealed record RefreshTokenRequest
{
    [Required(ErrorMessage = "Access token is required")]
    [MinLength(1, ErrorMessage = "Access token cannot be empty")]
    public required string AccessToken { get; init; }

    [Required(ErrorMessage = "Refresh token is required")]
    [MinLength(1, ErrorMessage = "Refresh token cannot be empty")]
    [MaxLength(500, ErrorMessage = "Refresh token cannot exceed 500 characters")]
    public required string RefreshToken { get; init; }
}

/// <summary>
/// Request for password change.
/// </summary>
public sealed record ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    [MinLength(1, ErrorMessage = "Current password cannot be empty")]
    [MaxLength(128, ErrorMessage = "Current password cannot exceed 128 characters")]
    public required string CurrentPassword { get; init; }

    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters")]
    [MaxLength(128, ErrorMessage = "New password cannot exceed 128 characters")]
    public required string NewPassword { get; init; }

    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public required string ConfirmNewPassword { get; init; }
}

/// <summary>
/// Request for forgot password.
/// </summary>
public sealed record ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
    public required string Email { get; init; }
}

/// <summary>
/// Request for password reset.
/// </summary>
public sealed record ResetPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Reset token is required")]
    [MinLength(1, ErrorMessage = "Reset token cannot be empty")]
    [MaxLength(500, ErrorMessage = "Reset token cannot exceed 500 characters")]
    public required string Token { get; init; }

    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters")]
    [MaxLength(128, ErrorMessage = "New password cannot exceed 128 characters")]
    public required string NewPassword { get; init; }

    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public required string ConfirmNewPassword { get; init; }
}

/// <summary>
/// Request for email verification.
/// </summary>
public sealed record VerifyEmailRequest
{
    [Required(ErrorMessage = "User ID is required")]
    [RegularExpression(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", 
        ErrorMessage = "Invalid User ID format")]
    public required string UserId { get; init; }

    [Required(ErrorMessage = "Verification token is required")]
    [MinLength(1, ErrorMessage = "Verification token cannot be empty")]
    [MaxLength(500, ErrorMessage = "Verification token cannot exceed 500 characters")]
    public required string Token { get; init; }
}
