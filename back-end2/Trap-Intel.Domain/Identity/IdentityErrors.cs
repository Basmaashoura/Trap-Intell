using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Identity
{
    /// <summary>
    /// Error definitions for the Identity domain.
    /// </summary>
    public static class IdentityErrors
    {
        // User Errors
        public static readonly Error UserNotFound = Error.Custom(
            "Identity.UserNotFound",
            "The specified user does not exist.");

        public static readonly Error UserEmailAlreadyExists = Error.Custom(
            "Identity.UserEmailAlreadyExists",
            "A user with this email already exists.");

        public static readonly Error UserNameAlreadyExists = Error.Custom(
            "Identity.UserNameAlreadyExists",
            "A user with this username already exists.");

        public static readonly Error InvalidUserEmail = Error.Custom(
            "Identity.InvalidUserEmail",
            "The user email is invalid.");

        public static readonly Error InvalidUserName = Error.Custom(
            "Identity.InvalidUserName",
            "The user name is invalid or too short.");

        public static readonly Error InvalidUserFirstName = Error.Custom(
            "Identity.InvalidUserFirstName",
            "The user first name is invalid.");

        public static readonly Error InvalidUserLastName = Error.Custom(
            "Identity.InvalidUserLastName",
            "The user last name is invalid.");

        public static readonly Error UserAlreadyActive = Error.Custom(
            "Identity.UserAlreadyActive",
            "The user is already active.");

        public static readonly Error UserAlreadyInactive = Error.Custom(
            "Identity.UserAlreadyInactive",
            "The user is already inactive.");

        public static readonly Error UserAlreadySuspended = Error.Custom(
            "Identity.UserAlreadySuspended",
            "The user is already suspended.");

        public static readonly Error UserCannotBeSuspended = Error.Custom(
            "Identity.UserCannotBeSuspended",
            "This user cannot be suspended.");

        public static readonly Error UserCannotBeDeleted = Error.Custom(
            "Identity.UserCannotBeDeleted",
            "This user cannot be deleted.");

        public static readonly Error InvalidOrganizationId = Error.Custom(
            "Identity.InvalidOrganizationId",
            "The organization ID is invalid.");

        public static readonly Error InvalidUserRole = Error.Custom(
            "Identity.InvalidUserRole",
            "The user role is invalid.");

        public static readonly Error CannotChangeRoleOfOnlyAdmin = Error.Custom(
            "Identity.CannotChangeRoleOfOnlyAdmin",
            "Cannot change the role of the only organization admin.");

        // User Profile Errors
        public static readonly Error InvalidPhoneNumber = Error.Custom(
            "Identity.InvalidPhoneNumber",
            "The phone number is invalid.");

        public static readonly Error InvalidUserPreferences = Error.Custom(
            "Identity.InvalidUserPreferences",
            "User preferences are invalid.");

        public static readonly Error InvalidNotificationSettings = Error.Custom(
            "Identity.InvalidNotificationSettings",
            "Notification settings are invalid.");

        public static Error UserNotFound_Detail(string userId)
        {
            return Error.Custom("Identity.UserNotFound", $"User '{userId}' not found.");
        }

        public static Error UserEmailAlreadyExists_Detail(string email)
        {
            return Error.Custom("Identity.UserEmailAlreadyExists", $"Email '{email}' is already registered.");
        }

        #region Authentication Errors

        public static readonly Error InvalidPassword = Error.Custom(
            "Identity.InvalidPassword",
            "Password is invalid.");

        public static readonly Error PasswordTooWeak = Error.Custom(
            "Identity.PasswordTooWeak",
            "Password does not meet complexity requirements.");

        public static readonly Error EmailAlreadyConfirmed = Error.Custom(
            "Identity.EmailAlreadyConfirmed",
            "Email is already confirmed.");

        public static readonly Error EmailNotConfirmed = Error.Custom(
            "Identity.EmailNotConfirmed",
            "Email must be confirmed before login.");

        public static readonly Error InvalidCredentials = Error.Custom(
            "Identity.InvalidCredentials",
            "Invalid email or password.");

        public static readonly Error AccountLocked = Error.Custom(
            "Identity.AccountLocked",
            "Account is locked. Try again later.");

        public static readonly Error AccountInactive = Error.Custom(
            "Identity.AccountInactive",
            "Account is not active. Please contact support.");

        public static readonly Error SuperAdminCannotBeLocked = Error.Custom(
            "Identity.SuperAdminCannotBeLocked",
            "Super admin accounts cannot be locked.");

        public static readonly Error TwoFactorRequired = Error.Custom(
            "Identity.TwoFactorRequired",
            "Two-factor authentication code required.");

        public static readonly Error InvalidTwoFactorCode = Error.Custom(
            "Identity.InvalidTwoFactorCode",
            "Invalid two-factor authentication code.");

        public static readonly Error InvalidRefreshToken = Error.Custom(
            "Identity.InvalidRefreshToken",
            "Refresh token is invalid or expired.");

        public static readonly Error RefreshTokenReused = Error.Custom(
            "Identity.RefreshTokenReused",
            "Refresh token reuse detected. All sessions revoked.");

        public static readonly Error SessionExpired = Error.Custom(
            "Identity.SessionExpired",
            "Session has expired. Please login again.");

        public static readonly Error InvalidEmailVerificationToken = Error.Custom(
            "Identity.InvalidEmailVerificationToken",
            "Email verification token is invalid or expired.");

        public static readonly Error InvalidPasswordResetToken = Error.Custom(
            "Identity.InvalidPasswordResetToken",
            "Password reset token is invalid or expired.");

        #endregion

        #region Email Verification Token Errors

        public static readonly Error EmailVerificationTokenNotFound = Error.Custom(
            "Identity.EmailVerificationTokenNotFound",
            "Email verification token not found.");

        public static readonly Error EmailVerificationTokenAlreadyUsed = Error.Custom(
            "Identity.EmailVerificationTokenAlreadyUsed",
            "Email verification token has already been used.");

        public static readonly Error EmailVerificationTokenRevoked = Error.Custom(
            "Identity.EmailVerificationTokenRevoked",
            "Email verification token has been revoked.");

        public static readonly Error EmailVerificationTokenExpired = Error.Custom(
            "Identity.EmailVerificationTokenExpired",
            "Email verification token has expired.");

        #endregion

        #region Password Reset Token Errors

        public static readonly Error PasswordResetTokenNotFound = Error.Custom(
            "Identity.PasswordResetTokenNotFound",
            "Password reset token not found.");

        public static readonly Error PasswordResetTokenAlreadyUsed = Error.Custom(
            "Identity.PasswordResetTokenAlreadyUsed",
            "Password reset token has already been used.");

        public static readonly Error PasswordResetTokenRevoked = Error.Custom(
            "Identity.PasswordResetTokenRevoked",
            "Password reset token has been revoked.");

        public static readonly Error PasswordResetTokenExpired = Error.Custom(
            "Identity.PasswordResetTokenExpired",
            "Password reset token has expired.");

        public static readonly Error PasswordResetTooManyRequests = Error.Custom(
            "Identity.PasswordResetTooManyRequests",
            "Too many password reset requests. Please try again later.");

        #endregion

        #region Two-Factor Authentication Errors

        public static readonly Error TwoFactorAlreadyEnabled = Error.Custom(
            "Identity.TwoFactorAlreadyEnabled",
            "Two-factor authentication is already enabled.");

        public static readonly Error TwoFactorNotEnabled = Error.Custom(
            "Identity.TwoFactorNotEnabled",
            "Two-factor authentication is not enabled.");

        public static readonly Error TwoFactorSetupRequired = Error.Custom(
            "Identity.TwoFactorSetupRequired",
            "Two-factor authentication setup is required. Please scan the QR code first.");

        public static readonly Error InvalidTwoFactorSetupToken = Error.Custom(
            "Identity.InvalidTwoFactorSetupToken",
            "Invalid or expired two-factor setup token.");

        public static readonly Error BackupCodeAlreadyUsed = Error.Custom(
            "Identity.BackupCodeAlreadyUsed",
            "This backup code has already been used.");

        public static readonly Error InvalidBackupCode = Error.Custom(
            "Identity.InvalidBackupCode",
            "Invalid backup code.");

        public static readonly Error NoBackupCodesRemaining = Error.Custom(
            "Identity.NoBackupCodesRemaining",
            "No backup codes remaining. Please generate new backup codes.");

        public static readonly Error TwoFactorVerificationFailed = Error.Custom(
            "Identity.TwoFactorVerificationFailed",
            "Two-factor verification failed. Please try again.");

        public static readonly Error InvalidTwoFactorToken = Error.Custom(
            "Identity.InvalidTwoFactorToken",
            "Invalid two-factor login token.");

        public static readonly Error TwoFactorTokenExpired = Error.Custom(
            "Identity.TwoFactorTokenExpired",
            "Two-factor login token has expired. Please login again.");

        #endregion
    }
}
