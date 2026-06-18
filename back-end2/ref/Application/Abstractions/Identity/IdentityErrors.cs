using SharedKernel;

namespace Application.Abstractions.Identity;

/// <summary>
/// Errors related to identity operations.
/// </summary>
public static class IdentityErrors
{
    public static readonly Error InvalidCredentials = Error.Failure(
        "Identity.InvalidCredentials",
        "Invalid email or password");

    public static readonly Error UserNotFound = Error.NotFound(
        "Identity.UserNotFound",
        "User not found");

    public static readonly Error UserLockedOut = Error.Failure(
        "Identity.UserLockedOut",
        "User account is locked out. Please try again later.");

    public static readonly Error UserDeactivated = Error.Failure(
        "Identity.UserDeactivated",
        "User account has been deactivated");

    public static readonly Error EmailNotConfirmed = Error.Failure(
        "Identity.EmailNotConfirmed",
        "Email address has not been confirmed");

    public static readonly Error TwoFactorRequired = Error.Failure(
        "Identity.TwoFactorRequired",
        "Two-factor authentication is required");

    public static readonly Error InvalidTwoFactorCode = Error.Failure(
        "Identity.InvalidTwoFactorCode",
        "Invalid two-factor authentication code");

    public static readonly Error TwoFactorNotEnabled = Error.Failure(
        "Identity.TwoFactorNotEnabled",
        "Two-factor authentication is not enabled for this account");

    public static readonly Error TwoFactorAlreadyEnabled = Error.Failure(
        "Identity.TwoFactorAlreadyEnabled",
        "Two-factor authentication is already enabled");

    public static readonly Error InvalidRefreshToken = Error.Failure(
        "Identity.InvalidRefreshToken",
        "Invalid or expired refresh token");

    public static readonly Error InvalidPasswordResetToken = Error.Failure(
        "Identity.InvalidPasswordResetToken",
        "Invalid or expired password reset token");

    public static readonly Error InvalidEmailConfirmationToken = Error.Failure(
        "Identity.InvalidEmailConfirmationToken",
        "Invalid or expired email confirmation token");

    public static readonly Error PasswordTooWeak = Error.Validation(
        "Identity.PasswordTooWeak",
        "Password does not meet security requirements");

    public static readonly Error EmailAlreadyExists = Error.Conflict(
        "Identity.EmailAlreadyExists",
        "A user with this email already exists");

    public static readonly Error RoleNotFound = Error.NotFound(
        "Identity.RoleNotFound",
        "Role not found");

    public static readonly Error RoleAlreadyExists = Error.Conflict(
        "Identity.RoleAlreadyExists",
        "A role with this name already exists");

    public static readonly Error UserAlreadyInRole = Error.Conflict(
        "Identity.UserAlreadyInRole",
        "User is already in this role");

    public static readonly Error UserNotInRole = Error.Failure(
        "Identity.UserNotInRole",
        "User is not in this role");

    public static readonly Error CannotRemoveLastAdmin = Error.Failure(
        "Identity.CannotRemoveLastAdmin",
        "Cannot remove the last Super Admin from the system");

    public static readonly Error PasswordMismatch = Error.Validation(
        "Identity.PasswordMismatch",
        "Current password is incorrect");

    public static readonly Error SamePassword = Error.Validation(
        "Identity.SamePassword",
        "New password must be different from current password");

    /// <summary>
    /// Creates an error from Identity error code and description.
    /// </summary>
    public static Error FromIdentityError(string code, string description) => Error.Failure(
        $"Identity.{code}",
        description);
}
