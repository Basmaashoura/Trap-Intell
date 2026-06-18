namespace Trap_Intel.Application.Abstractions.Identity;

public sealed record OrganizationInvitationEmailMessage(
    string RecipientEmail,
    string RecipientDisplayName,
    string OrganizationName,
    string RoleName,
    string InvitationToken,
    string? PersonalMessage,
    DateTime ExpiresAt);

public sealed record OrganizationWelcomeEmailMessage(
    string RecipientEmail,
    string RecipientDisplayName,
    string OrganizationName,
    string RoleName);

public interface IOrganizationEmailService
{
    Task SendOrganizationInvitationAsync(
        OrganizationInvitationEmailMessage message,
        CancellationToken cancellationToken = default);

    Task SendOrganizationWelcomeAsync(
        OrganizationWelcomeEmailMessage message,
        CancellationToken cancellationToken = default);
}
