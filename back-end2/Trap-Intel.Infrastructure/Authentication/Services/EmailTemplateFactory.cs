using System.Net;
using System.Text;

namespace Trap_Intel.Infrastructure.Authentication.Services;

internal static class EmailTemplateFactory
{
    private const string BrandColor = "#0a6d63";
    private const string BrandColorDark = "#07554d";
    private const string BackgroundColor = "#f3f8f6";
    private const string CardBackgroundColor = "#ffffff";
    private const string TextColor = "#122026";
    private const string MutedTextColor = "#526572";

    public static string BuildEmailVerificationTemplate(string userName, string verificationLink)
    {
        var safeName = Encode(userName);
        var safeLink = EncodeAttribute(verificationLink);

        var content = $@"
            <p style='margin:0 0 14px;color:{MutedTextColor};line-height:1.6;'>Hi {safeName},</p>
            <p style='margin:0 0 14px;color:{TextColor};line-height:1.6;'>Welcome to Trap-Intel. Verify your email to activate your account and complete onboarding.</p>
            {BuildPrimaryButton(safeLink, "Verify My Email")}
            <p style='margin:16px 0 0;color:{MutedTextColor};line-height:1.6;'>If you did not create this account, you can safely ignore this email.</p>";

        return BuildLayout("Verify your account", "Activate your Trap-Intel account", content, "Security first onboarding");
    }

    public static string BuildPasswordResetTemplate(string userName, string resetLink)
    {
        var safeName = Encode(userName);
        var safeLink = EncodeAttribute(resetLink);

        var content = $@"
            <p style='margin:0 0 14px;color:{MutedTextColor};line-height:1.6;'>Hi {safeName},</p>
            <p style='margin:0 0 14px;color:{TextColor};line-height:1.6;'>We received a request to reset your password. Use the secure link below to choose a new password.</p>
            {BuildPrimaryButton(safeLink, "Reset Password")}
            <p style='margin:16px 0 0;color:{MutedTextColor};line-height:1.6;'>This reset link is time-limited for your protection. If you did not request this action, ignore this message.</p>";

        return BuildLayout("Password reset", "Reset your Trap-Intel password", content, "Account security notification");
    }

    public static string BuildPasswordChangedTemplate(string userName, string? ipAddress)
    {
        var safeName = Encode(userName);
        var safeIpAddress = Encode(string.IsNullOrWhiteSpace(ipAddress) ? "Unknown" : ipAddress);

        var content = $@"
            <p style='margin:0 0 14px;color:{MutedTextColor};line-height:1.6;'>Hi {safeName},</p>
            <p style='margin:0 0 14px;color:{TextColor};line-height:1.6;'>Your account password was changed successfully.</p>
            <div style='margin:0 0 14px;padding:12px 14px;background:#edf5ff;border:1px solid #cdddf4;border-radius:10px;color:{TextColor};'>
                <strong>Source IP:</strong> {safeIpAddress}
            </div>
            <p style='margin:0;color:{MutedTextColor};line-height:1.6;'>If this was not you, contact your security administrator immediately.</p>";

        return BuildLayout("Password updated", "Your password has changed", content, "Security event");
    }

    public static string BuildWelcomeTemplate(string userName)
    {
        var safeName = Encode(userName);

        var content = $@"
            <p style='margin:0 0 14px;color:{MutedTextColor};line-height:1.6;'>Hi {safeName},</p>
            <p style='margin:0 0 14px;color:{TextColor};line-height:1.6;'>Welcome to Trap-Intel. Your account is ready and verified.</p>
            <p style='margin:0;color:{MutedTextColor};line-height:1.6;'>You can now monitor threats, manage alerts, and secure your organization from one unified platform.</p>";

        return BuildLayout("Welcome aboard", "Your Trap-Intel account is active", content, "Security operations platform");
    }

    public static string BuildSecurityAlertTemplate(string userName, string alertType, string details)
    {
        var safeName = Encode(userName);
        var safeAlertType = Encode(alertType);
        var safeDetails = Encode(details)
            .Replace("\r\n", "<br/>")
            .Replace("\n", "<br/>");
        var safeTimestamp = Encode(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"));

        var content = $@"
            <p style='margin:0 0 14px;color:{MutedTextColor};line-height:1.6;'>Hi {safeName},</p>
            <p style='margin:0 0 14px;color:{TextColor};line-height:1.6;'>A platform alert requires attention. Please review the details below.</p>
            <div style='margin:0 0 14px;padding:14px;background:#fff7ed;border:1px solid #f6d7b2;border-radius:12px;'>
                <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%'>
                    <tr>
                        <td style='padding:0 0 6px;color:{MutedTextColor};font-size:13px;'>Alert type</td>
                        <td style='padding:0 0 6px;color:{TextColor};font-weight:600;font-size:13px;text-align:right;'>{safeAlertType}</td>
                    </tr>
                    <tr>
                        <td style='padding:0;color:{MutedTextColor};font-size:13px;'>Detected at</td>
                        <td style='padding:0;color:{TextColor};font-weight:600;font-size:13px;text-align:right;'>{safeTimestamp}</td>
                    </tr>
                </table>
            </div>
            <div style='margin:0 0 14px;padding:14px;background:#0f172a;border:1px solid #1e293b;border-radius:12px;'>
                <div style='margin:0 0 8px;color:#93c5fd;font-size:12px;letter-spacing:0.5px;text-transform:uppercase;'>Incident details</div>
                <div style='margin:0;color:#e2e8f0;line-height:1.7;font-size:14px;'>{safeDetails}</div>
            </div>
            <p style='margin:0;color:{MutedTextColor};line-height:1.6;'>If this activity is unfamiliar, investigate immediately and rotate affected credentials.</p>";

        return BuildLayout("Security alert", "Important security notification", content, "High priority event");
    }

    public static string BuildPlatformNotificationTemplate(string userName, string title, string details)
    {
        var safeName = Encode(userName);
        var safeTitle = Encode(title);
        var safeDetails = Encode(details)
            .Replace("\r\n", "<br/>")
            .Replace("\n", "<br/>");
        var safeTimestamp = Encode(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"));

        var content = $@"
            <p style='margin:0 0 14px;color:{MutedTextColor};line-height:1.6;'>Hi {safeName},</p>
            <p style='margin:0 0 14px;color:{TextColor};line-height:1.6;'>You have a new platform notification from Trap-Intel.</p>
            <div style='margin:0 0 14px;padding:14px;background:#eef7ff;border:1px solid #cfe2f3;border-radius:12px;'>
                <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%'>
                    <tr>
                        <td style='padding:0 0 6px;color:{MutedTextColor};font-size:13px;'>Title</td>
                        <td style='padding:0 0 6px;color:{TextColor};font-weight:600;font-size:13px;text-align:right;'>{safeTitle}</td>
                    </tr>
                    <tr>
                        <td style='padding:0;color:{MutedTextColor};font-size:13px;'>Created at</td>
                        <td style='padding:0;color:{TextColor};font-weight:600;font-size:13px;text-align:right;'>{safeTimestamp}</td>
                    </tr>
                </table>
            </div>
            <div style='margin:0 0 14px;padding:14px;background:#ffffff;border:1px solid #d9e5e2;border-radius:12px;'>
                <div style='margin:0 0 8px;color:#1d4f63;font-size:12px;letter-spacing:0.5px;text-transform:uppercase;'>Details</div>
                <div style='margin:0;color:{TextColor};line-height:1.7;font-size:14px;'>{safeDetails}</div>
            </div>
            <p style='margin:0;color:{MutedTextColor};line-height:1.6;'>You can also review this event directly from your Trap-Intel dashboard.</p>";

        return BuildLayout("Platform notification", "Operational update", content, "System notification");
    }

    public static string BuildOrganizationInvitationTemplate(
        string recipientName,
        string organizationName,
        string roleName,
        string invitationLink,
        string? personalMessage,
        DateTime expiresAtUtc)
    {
        var safeRecipientName = Encode(recipientName);
        var safeOrganizationName = Encode(organizationName);
        var safeRoleName = Encode(roleName);
        var safeInvitationLink = EncodeAttribute(invitationLink);
        var safeExpiration = Encode(expiresAtUtc.ToString("yyyy-MM-dd HH:mm 'UTC'"));

        string personalMessageBlock = string.Empty;
        if (!string.IsNullOrWhiteSpace(personalMessage))
        {
            var safeMessage = Encode(personalMessage.Trim());
            personalMessageBlock = $@"
                <div style='margin:0 0 14px;padding:12px 14px;background:#f7fbff;border:1px solid #d6e6f7;border-radius:10px;color:{TextColor};line-height:1.6;'>
                    <strong>Personal message:</strong><br/>{safeMessage}
                </div>";
        }

        var content = $@"
            <p style='margin:0 0 14px;color:{MutedTextColor};line-height:1.6;'>Hi {safeRecipientName},</p>
            <p style='margin:0 0 14px;color:{TextColor};line-height:1.6;'>You have been invited to join <strong>{safeOrganizationName}</strong> on Trap-Intel with role <strong>{safeRoleName}</strong>.</p>
            {personalMessageBlock}
            <div style='margin:0 0 14px;padding:12px 14px;background:#edf8f5;border:1px solid #cbe6dd;border-radius:10px;color:{TextColor};'>
                <strong>Invitation expires:</strong> {safeExpiration}
            </div>
            {BuildPrimaryButton(safeInvitationLink, "Accept Invitation")}
            <p style='margin:16px 0 0;color:{MutedTextColor};line-height:1.6;'>If this invitation was unexpected, you can ignore this email.</p>";

        return BuildLayout("Organization invitation", "You are invited to join a security workspace", content, "Team onboarding");
    }

    public static string BuildOrganizationWelcomeTemplate(
        string recipientName,
        string organizationName,
        string roleName)
    {
        var safeRecipientName = Encode(recipientName);
        var safeOrganizationName = Encode(organizationName);
        var safeRoleName = Encode(roleName);

        var content = $@"
            <p style='margin:0 0 14px;color:{MutedTextColor};line-height:1.6;'>Hi {safeRecipientName},</p>
            <p style='margin:0 0 14px;color:{TextColor};line-height:1.6;'>Welcome to <strong>{safeOrganizationName}</strong>. Your assigned role is <strong>{safeRoleName}</strong>.</p>
            <p style='margin:0;color:{MutedTextColor};line-height:1.6;'>You now have access to your organization workspace including alerts, roles, and security notifications based on your permissions.</p>";

        return BuildLayout("Welcome to your organization", "Organization access granted", content, "Secure collaboration");
    }

    private static string BuildLayout(string heading, string subHeading, string contentHtml, string footerTag)
    {
        var safeHeading = Encode(heading);
        var safeSubHeading = Encode(subHeading);
        var safeFooterTag = Encode(footerTag);

        return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8'/>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
  <title>{safeHeading}</title>
</head>
<body style='margin:0;padding:0;background:{BackgroundColor};font-family:Segoe UI, Tahoma, Arial, sans-serif;color:{TextColor};'>
  <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='background:{BackgroundColor};padding:20px 0;'>
    <tr>
      <td align='center'>
        <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='max-width:640px;margin:0 auto;'>
          <tr>
            <td style='padding:0 16px;'>
              <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='background:{CardBackgroundColor};border:1px solid #d9e5e2;border-radius:16px;overflow:hidden;'>
                <tr>
                  <td style='padding:22px 24px;background:linear-gradient(135deg,{BrandColorDark},{BrandColor});color:#ffffff;'>
                    <div style='font-size:20px;font-weight:700;letter-spacing:0.3px;'>Trap-Intel</div>
                    <div style='font-size:14px;opacity:0.9;margin-top:6px;'>{safeSubHeading}</div>
                  </td>
                </tr>
                <tr>
                  <td style='padding:24px;'>
                    <h1 style='margin:0 0 14px;font-size:22px;color:{TextColor};'>{safeHeading}</h1>
                    {contentHtml}
                  </td>
                </tr>
              </table>
              <div style='padding:14px 8px 0;font-size:12px;color:#6b7d89;text-align:center;'>
                {safeFooterTag} | Trap-Intel Security Platform
              </div>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    private static string BuildPrimaryButton(string link, string label)
    {
        var safeLabel = Encode(label);
        return $@"
            <table role='presentation' cellspacing='0' cellpadding='0' border='0' style='margin:18px 0;'>
                <tr>
                    <td style='border-radius:10px;background:{BrandColor};'>
                        <a href='{link}' style='display:inline-block;padding:11px 18px;color:#ffffff;text-decoration:none;font-weight:600;'>
                            {safeLabel}
                        </a>
                    </td>
                </tr>
            </table>";
    }

    private static string Encode(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    private static string EncodeAttribute(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var encoded = WebUtility.HtmlEncode(value.Trim());

        // Prevent line-break attribute injection in case input carries control characters.
        var sb = new StringBuilder(encoded.Length);
        foreach (var ch in encoded)
        {
            if (!char.IsControl(ch))
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }
}
