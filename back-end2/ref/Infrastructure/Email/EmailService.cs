using Application.Abstractions.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Email;

/// <summary>
/// Email service implementation using MailKit.
/// Supports SMTP sending with MailPit for development.
/// </summary>
internal sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _settings;

    public EmailService(
        ILogger<EmailService> logger,
        IOptions<EmailSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        string resetLink = $"{_settings.AppBaseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(toEmail)}";

        string htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #888; font-size: 12px; margin-top: 20px; }}
        .warning {{ background: #fff3cd; border: 1px solid #ffc107; padding: 15px; border-radius: 5px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? Password Reset Request</h1>
        </div>
        <div class='content'>
            <p>Hello,</p>
            <p>We received a request to reset your password for your ONEX account.</p>
            <p>Click the button below to reset your password:</p>
            <p style='text-align: center;'>
                <a href='{resetLink}' class='button'>Reset Password</a>
            </p>
            <div class='warning'>
                <strong>?? Security Notice:</strong>
                <ul>
                    <li>This link will expire in <b>20 minutes</b></li>
                    <li>This link can only be used <b>once</b></li>
                    <li>If you didn't request this, please ignore this email</li>
                    <li>Never share this link with anyone</li>
                </ul>
            </div>
            <p>Or copy and paste this link in your browser:</p>
            <p style='word-break: break-all; font-size: 12px; color: #666;'>{resetLink}</p>
        </div>
        <div class='footer'>
            <p>© 2024 ONEX. All rights reserved.</p>
            <p>This is an automated message, please do not reply.</p>
        </div>
    </div>
</body>
</html>";

        string plainText = $@"
Password Reset Request

Hello,

We received a request to reset your password for your ONEX account.

Click the link below to reset your password:
{resetLink}

Security Notice:
- This link will expire in 20 minutes
- This link can only be used once
- If you didn't request this, please ignore this email
- Never share this link with anyone

© 2024 ONEX. All rights reserved.
";

        await SendEmailCoreAsync(
            toEmail,
            "Reset Your ONEX Password",
            htmlBody,
            plainText,
            cancellationToken);
    }

    public async Task SendEmailConfirmationAsync(
        string toEmail,
        string confirmationToken,
        CancellationToken cancellationToken = default)
    {
        string confirmLink = $"{_settings.AppBaseUrl}/confirm-email?token={Uri.EscapeDataString(confirmationToken)}&email={Uri.EscapeDataString(toEmail)}";

        string htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; background: #11998e; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #888; font-size: 12px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? Welcome to ONEX!</h1>
        </div>
        <div class='content'>
            <p>Hello,</p>
            <p>Thank you for registering with ONEX. Please confirm your email address to activate your account.</p>
            <p style='text-align: center;'>
                <a href='{confirmLink}' class='button'>Confirm Email</a>
            </p>
            <p>Or copy and paste this link in your browser:</p>
            <p style='word-break: break-all; font-size: 12px; color: #666;'>{confirmLink}</p>
            <p>If you didn't create an account with us, please ignore this email.</p>
        </div>
        <div class='footer'>
            <p>© 2024 ONEX. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        string plainText = $@"
Welcome to ONEX!

Hello,

Thank you for registering with ONEX. Please confirm your email address to activate your account.

Click the link below to confirm your email:
{confirmLink}


If you didn't create an account with us, please ignore this email.

© 2024 ONEX. All rights reserved.
";

        await SendEmailCoreAsync(
            toEmail,
            "Confirm Your ONEX Email",
            htmlBody,
            plainText,
            cancellationToken);
    }

    public async Task SendContactInvitationAsync(
        string toEmail,
        string contactName,
        string accountName,
        string inviterName,
        string invitationToken,
        Guid invitationId,
        bool isNewUser,
        CancellationToken cancellationToken = default)
    {
        string acceptLink = $"{_settings.AppBaseUrl}/accept-invitation?token={Uri.EscapeDataString(invitationToken)}&id={invitationId}";

        string actionText = isNewUser
            ? "create your account and join"
            : "join";


        string htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; background: #6366f1; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #888; font-size: 12px; margin-top: 20px; }}
        .info-box {{ background: #e0e7ff; border: 1px solid #6366f1; padding: 15px; border-radius: 5px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? You're Invited!</h1>
        </div>
        <div class='content'>
            <p>Hello {contactName},</p>
            <p><strong>{inviterName}</strong> has invited you to {actionText} <strong>{accountName}</strong> on ONEX.</p>
            
            <div class='info-box'>
                <strong>?? What happens next:</strong>
                <ul>
                    <li>Accept the invitation below</li>
                    <li>Set up your account password</li>
                    <li>Your administrator will configure your access permissions</li>
                </ul>
            </div>

            <p style='text-align: center;'>
                <a href='{acceptLink}' class='button'>Accept Invitation</a>
            </p>

            <p style='font-size: 12px; color: #666;'>
                ? This invitation expires in 7 days.<br>
                If you didn't expect this invitation, you can safely ignore this email.
            </p>

            <p>Or copy and paste this link in your browser:</p>
            <p style='word-break: break-all; font-size: 12px; color: #666;'>{acceptLink}</p>
        </div>
        <div class='footer'>
            <p>© 2024 ONEX. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        string plainText = $@"
You're Invited to {accountName}!

Hello {contactName},

{inviterName} has invited you to {actionText} {accountName} on ONEX.

Click the link below to accept the invitation:
{acceptLink}

This invitation expires in 7 days.
If you didn't expect this invitation, you can safely ignore this email.

© 2024 ONEX. All rights reserved.
";

        await SendEmailCoreAsync(
            toEmail,
            $"You're invited to join {accountName} on ONEX",
            htmlBody,
            plainText,
            cancellationToken);
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        await SendEmailCoreAsync(toEmail, subject, htmlBody, null, cancellationToken);
    }

    private async Task SendEmailCoreAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody,
        CancellationToken cancellationToken)
    {
        if (!_settings.EnableSending)
        {
            _logger.LogInformation(
                "Email sending disabled. Would send to {Email} with subject: {Subject}",
                toEmail,
                subject);
            return;
        }

        try
        {
            // Create the message
            using var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            // Build the body with both HTML and plain text versions
            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            if (!string.IsNullOrEmpty(plainTextBody))
            {
                builder.TextBody = plainTextBody;
            }

            message.Body = builder.ToMessageBody();

            // Send using SMTP
            using var client = new SmtpClient();

            // Determine security options based on settings
            SecureSocketOptions securityOptions = _settings.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(
                _settings.SmtpHost,
                _settings.SmtpPort,
                securityOptions,
                cancellationToken);

            // Authenticate if credentials provided
            if (!string.IsNullOrEmpty(_settings.Username) && !string.IsNullOrEmpty(_settings.Password))
            {
                await client.AuthenticateAsync(
                    _settings.Username,
                    _settings.Password,
                    cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);

            _logger.LogInformation(
                "Email sent successfully to {Email} with subject: {Subject}",
                toEmail,
                subject);

            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {Email} with subject: {Subject}",
                toEmail,
                subject);

            // Don't throw - email failures shouldn't break the main flow
            // In production, you might want to queue for retry
        }
    }
}
