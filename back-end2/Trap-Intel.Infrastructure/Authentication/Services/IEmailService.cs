namespace Trap_Intel.Infrastructure.Authentication.Services;

/// <summary>
/// Email service interface for sending authentication-related emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email verification link to the user.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="userName">The user's display name.</param>
    /// <param name="verificationToken">The verification token to include in the link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendEmailVerificationAsync(
        string email,
        string userName,
        string verificationToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset link to the user.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="userName">The user's display name.</param>
    /// <param name="resetToken">The password reset token to include in the link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasswordResetAsync(
        string email,
        string userName,
        string resetToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification that the password was changed.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="userName">The user's display name.</param>
    /// <param name="ipAddress">IP address where the change was made.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasswordChangedNotificationAsync(
        string email,
        string userName,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a welcome email after successful registration and email verification.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="userName">The user's display name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendWelcomeEmailAsync(
        string email,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a security alert email (suspicious activity, new login from different location, etc.).
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="userName">The user's display name.</param>
    /// <param name="alertType">Type of security alert.</param>
    /// <param name="details">Additional details about the alert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendSecurityAlertAsync(
        string email,
        string userName,
        string alertType,
        string details,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a general platform notification email for non-security events.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="userName">The user's display name.</param>
    /// <param name="title">Notification title.</param>
    /// <param name="details">Additional details about the notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPlatformNotificationAsync(
        string email,
        string userName,
        string title,
        string details,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an organization invitation email with secure acceptance link.
    /// </summary>
    Task SendOrganizationInvitationAsync(
        string email,
        string recipientName,
        string organizationName,
        string roleName,
        string invitationToken,
        string? personalMessage,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an organization-specific welcome email after invitation acceptance.
    /// </summary>
    Task SendOrganizationWelcomeAsync(
        string email,
        string recipientName,
        string organizationName,
        string roleName,
        CancellationToken cancellationToken = default);
}
