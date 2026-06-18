using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.Identity;

namespace Trap_Intel.Infrastructure.Authentication.Services;

public sealed class OrganizationEmailService : IOrganizationEmailService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<OrganizationEmailService> _logger;

    public OrganizationEmailService(IEmailService emailService, ILogger<OrganizationEmailService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendOrganizationInvitationAsync(
        OrganizationInvitationEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.RecipientEmail) ||
            string.IsNullOrWhiteSpace(message.InvitationToken))
        {
            return;
        }



        var recipientName = string.IsNullOrWhiteSpace(message.RecipientDisplayName)
            ? ResolveFallbackNameFromEmail(message.RecipientEmail)
            : message.RecipientDisplayName.Trim();

        try
        {
            await _emailService.SendOrganizationInvitationAsync(
                message.RecipientEmail,
                recipientName,
                message.OrganizationName,
                message.RoleName,
                message.InvitationToken,
                message.PersonalMessage,
                message.ExpiresAt,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Organization invitation email failed for recipient {RecipientEmail}.",
                message.RecipientEmail);
        }
    }

    public async Task SendOrganizationWelcomeAsync(
        OrganizationWelcomeEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.RecipientEmail))
        {
            return;
        }

        var recipientName = string.IsNullOrWhiteSpace(message.RecipientDisplayName)
            ? ResolveFallbackNameFromEmail(message.RecipientEmail)
            : message.RecipientDisplayName.Trim();

        try
        {
            await _emailService.SendOrganizationWelcomeAsync(
                message.RecipientEmail,
                recipientName,
                message.OrganizationName,
                message.RoleName,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Organization welcome email failed for recipient {RecipientEmail}.",
                message.RecipientEmail);
        }
    }

    private static string ResolveFallbackNameFromEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return "User";
        }

        var local = email[..atIndex].Trim();
        if (string.IsNullOrWhiteSpace(local))
        {
            return "User";
        }

        return local.Length <= 80 ? local : local[..80];
    }
}
