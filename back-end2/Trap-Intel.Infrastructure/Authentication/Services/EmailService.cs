using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trap_Intel.Infrastructure.Authentication.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Trap_Intel.Infrastructure.Authentication.Services;

/// <summary>
/// Email service implementation using MailKit for SMTP delivery.
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> settings,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(
        string email,
        string userName,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        var verificationLink = BuildVerificationLink(verificationToken);
        var subject = "Verify Your Account - Trap-Intel";
        var body = EmailTemplateFactory.BuildEmailVerificationTemplate(userName, verificationLink);

        _logger.LogInformation("Sending verification email to {Email}", email);
        await SendEmailAsync(email, userName, subject, body, cancellationToken);
    }

    public async Task SendPasswordResetAsync(
        string email,
        string userName,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        var resetLink = BuildPasswordResetLink(resetToken);
        var subject = "Password Reset Request - Trap-Intel";
        var body = EmailTemplateFactory.BuildPasswordResetTemplate(userName, resetLink);

        _logger.LogInformation("Sending password reset to {Email}", email);
        await SendEmailAsync(email, userName, subject, body, cancellationToken);
    }

    public async Task SendPasswordChangedNotificationAsync(
        string email,
        string userName,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var subject = "Password Changed - Trap-Intel";
        var body = EmailTemplateFactory.BuildPasswordChangedTemplate(userName, ipAddress);

        await SendEmailAsync(email, userName, subject, body, cancellationToken);
    }

    public async Task SendWelcomeEmailAsync(
        string email,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to Trap-Intel!";
        var body = EmailTemplateFactory.BuildWelcomeTemplate(userName);

        await SendEmailAsync(email, userName, subject, body, cancellationToken);
    }

    public async Task SendSecurityAlertAsync(
        string email,
        string userName,
        string alertType,
        string details,
        CancellationToken cancellationToken = default)
    {
        var subject = $"[SECURITY ALERT] {alertType}";
        var body = EmailTemplateFactory.BuildSecurityAlertTemplate(userName, alertType, details);

        _logger.LogWarning("Sending critical security email alert to {Email}", email);
        await SendEmailAsync(email, userName, subject, body, cancellationToken);
    }

    public async Task SendPlatformNotificationAsync(
        string email,
        string userName,
        string title,
        string details,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = string.IsNullOrWhiteSpace(title)
            ? "Trap-Intel notification"
            : title.Trim();

        var subject = $"[Trap-Intel] {normalizedTitle}";
        var body = EmailTemplateFactory.BuildPlatformNotificationTemplate(userName, normalizedTitle, details);

        _logger.LogInformation("Sending platform notification email to {Email}", email);
        await SendEmailAsync(email, userName, subject, body, cancellationToken);
    }

    public async Task SendOrganizationInvitationAsync(
        string email,
        string recipientName,
        string organizationName,
        string roleName,
        string invitationToken,
        string? personalMessage,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var invitationLink = BuildOrganizationInvitationLink(invitationToken);
        var subject = $"Invitation to join {organizationName} on Trap-Intel";
        var body = EmailTemplateFactory.BuildOrganizationInvitationTemplate(
            recipientName,
            organizationName,
            roleName,
            invitationLink,
            personalMessage,
            expiresAt);

        _logger.LogInformation(
            "Sending organization invitation email to {Email}. Org={OrganizationName}, Role={RoleName}, ExpiresAt={ExpiresAt}",
            email,
            organizationName,
            roleName,
            expiresAt);

        await SendEmailAsync(email, recipientName, subject, body, cancellationToken);
    }

    public async Task SendOrganizationWelcomeAsync(
        string email,
        string recipientName,
        string organizationName,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        var subject = $"Welcome to {organizationName} on Trap-Intel";
        var body = EmailTemplateFactory.BuildOrganizationWelcomeTemplate(recipientName, organizationName, roleName);

        _logger.LogInformation(
            "Sending organization welcome email to {Email}. Org={OrganizationName}, Role={RoleName}",
            email,
            organizationName,
            roleName);

        await SendEmailAsync(email, recipientName, subject, body, cancellationToken);
    }

    private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Handle SSL/TLS logic per settings
            var secureSocketOptions = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

            // Optional: Bypass certificate validation for local development testing
            // client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            // Connect and Authenticate before sending
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, secureSocketOptions, cancellationToken);

            if (!string.IsNullOrEmpty(_settings.SmtpUsername) && !string.IsNullOrEmpty(_settings.SmtpPassword))
            {
                await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully. Type: {Subject}, To: {Email}", subject, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email. Type: {Subject}, To: {Email}", subject, toEmail);
            // Swallowing exceptions so it doesn't block commands in DEV
        }
    }

    private string BuildVerificationLink(string token)
    {
        var baseUrl = _settings.FrontendBaseUrl.TrimEnd('/');
        var path = _settings.EmailVerificationPath.TrimStart('/');
        return $"{baseUrl}/{path}?token={Uri.EscapeDataString(token)}";
    }

    private string BuildPasswordResetLink(string token)
    {
        var baseUrl = _settings.FrontendBaseUrl.TrimEnd('/');
        var path = _settings.PasswordResetPath.TrimStart('/');
        return $"{baseUrl}/{path}?token={Uri.EscapeDataString(token)}";
    }

    private string BuildOrganizationInvitationLink(string token)
    {
        var baseUrl = _settings.FrontendBaseUrl.TrimEnd('/');
        var path = _settings.OrganizationInvitationPath.TrimStart('/');
        return $"{baseUrl}/{path}?token={Uri.EscapeDataString(token)}";
    }
}
