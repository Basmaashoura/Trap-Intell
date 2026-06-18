namespace Application.Abstractions.Email;

/// <summary>
/// Service for sending emails.
/// Application layer abstraction - implementation in Infrastructure.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a password reset email.
    /// </summary>
    Task SendPasswordResetEmailAsync(
        string toEmail,
        string resetToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email confirmation email.
    /// </summary>
    Task SendEmailConfirmationAsync(
        string toEmail,
        string confirmationToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a contact invitation email.
    /// </summary>
    Task SendContactInvitationAsync(
        string toEmail,
        string contactName,
        string accountName,
        string inviterName,
        string invitationToken,
        Guid invitationId,
        bool isNewUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a generic email.
    /// </summary>
    Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
