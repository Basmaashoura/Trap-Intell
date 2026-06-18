using MediatR;
using Microsoft.Extensions.Logging;
using System.Text;
using Trap_Intel.Application.Abstractions.Auditing;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Domain.Invitations.Events;

namespace Trap_Intel.Application.Organizations.Events;

internal sealed class InvitationDomainEventAuditHandler :
    INotificationHandler<InvitationCreatedEvent>,
    INotificationHandler<InvitationAcceptedEvent>,
    INotificationHandler<InvitationDeclinedEvent>,
    INotificationHandler<InvitationRevokedEvent>,
    INotificationHandler<InvitationExpiredEvent>,
    INotificationHandler<InvitationReminderSentEvent>,
    INotificationHandler<InvitationExpirationExtendedEvent>,
    INotificationHandler<InvitationRoleUpdatedEvent>,
    INotificationHandler<InvitationResentEvent>,
    INotificationHandler<BulkInvitationsSentEvent>
{
    private const int MaxAuditReasonLength = 1000;
    private const int MaxAuditFieldLength = 160;

    private readonly IAuditService _auditService;
    private readonly ILogger<InvitationDomainEventAuditHandler> _logger;

    public InvitationDomainEventAuditHandler(
        IAuditService auditService,
        ILogger<InvitationDomainEventAuditHandler> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public Task Handle(InvitationCreatedEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.InvitationId,
            AuditAction.Create,
            AuditSeverity.Info,
            $"Invitation created for {MaskEmail(notification.Email)}. RoleId={SanitizeAuditValue(notification.RoleId.ToString(), MaxAuditFieldLength)}.",
            cancellationToken);
    }

    public Task Handle(InvitationAcceptedEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.InvitationId,
            AuditAction.Approve,
            AuditSeverity.Info,
            $"Invitation accepted by user {SanitizeAuditValue(notification.AcceptedByUserId.ToString(), MaxAuditFieldLength)} for {MaskEmail(notification.Email)}.",
            cancellationToken);
    }

    public Task Handle(InvitationDeclinedEvent notification, CancellationToken cancellationToken)
    {
        var reason = string.IsNullOrWhiteSpace(notification.Reason)
            ? "No decline reason provided"
            : notification.Reason;

        return RecordAsync(
            notification.OrganizationId,
            notification.InvitationId,
            AuditAction.Reject,
            AuditSeverity.Warning,
                $"Invitation declined by {MaskEmail(notification.Email)}. Reason: {SanitizeAuditValue(reason, MaxAuditFieldLength)}",
            cancellationToken);
    }

    public Task Handle(InvitationRevokedEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.InvitationId,
            AuditAction.Cancel,
            AuditSeverity.Warning,
            $"Invitation revoked for {MaskEmail(notification.Email)} by user {SanitizeAuditValue(notification.RevokedByUserId.ToString(), MaxAuditFieldLength)}. Reason: {SanitizeAuditValue(notification.Reason, MaxAuditFieldLength)}",
            cancellationToken);
    }

    public Task Handle(InvitationExpiredEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.InvitationId,
            AuditAction.Archive,
            AuditSeverity.Warning,
            $"Invitation expired for {MaskEmail(notification.Email)}.",
            cancellationToken);
    }

    public Task Handle(InvitationReminderSentEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.InvitationId,
            AuditAction.View,
            AuditSeverity.Info,
            $"Invitation reminder #{notification.ReminderNumber} sent to {MaskEmail(notification.Email)}.",
            cancellationToken);
    }

    public Task Handle(InvitationExpirationExtendedEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.InvitationId,
            AuditAction.Update,
            AuditSeverity.Info,
            $"Invitation expiration extended for {MaskEmail(notification.Email)} until {notification.NewExpiresAt:O}.",
            cancellationToken);
    }

    public Task Handle(InvitationRoleUpdatedEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.InvitationId,
            AuditAction.Update,
            AuditSeverity.Info,
            $"Invitation role updated for {MaskEmail(notification.Email)}: {SanitizeAuditValue(notification.OldRoleId.ToString(), MaxAuditFieldLength)} -> {SanitizeAuditValue(notification.NewRoleId.ToString(), MaxAuditFieldLength)} by user {SanitizeAuditValue(notification.UpdatedByUserId.ToString(), MaxAuditFieldLength)}.",
            cancellationToken);
    }

    public Task Handle(InvitationResentEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.InvitationId,
            AuditAction.Update,
            AuditSeverity.Info,
            $"Invitation resent to {MaskEmail(notification.Email)} by user {SanitizeAuditValue(notification.ResentByUserId.ToString(), MaxAuditFieldLength)}. New expiry: {notification.NewExpiresAt:O}.",
            cancellationToken);
    }

    public Task Handle(BulkInvitationsSentEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.OrganizationId,
            AuditAction.Create,
            AuditSeverity.Info,
            $"Bulk invitations sent by user {notification.SentByUserId}. Total={notification.TotalSent}, Success={notification.SuccessCount}, Failed={notification.FailedCount}, RoleId={notification.RoleId}.",
            cancellationToken);
    }

    private async Task RecordAsync(
        Guid organizationId,
        Guid resourceId,
        AuditAction action,
        AuditSeverity severity,
        string reason,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (organizationId == Guid.Empty || resourceId == Guid.Empty)
        {
            _logger.LogWarning(
                "Skipping invitation audit event due to invalid ids. OrgId={OrgId}, ResourceId={ResourceId}, Action={Action}",
                organizationId,
                resourceId,
                action);
            return;
        }

        try
        {
            await _auditService.RecordAsync(
                organizationId,
                AuditResourceType.Organization,
                resourceId,
                action,
                severity,
                SanitizeAuditValue(reason, MaxAuditReasonLength),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to persist invitation audit record. OrgId={OrgId}, ResourceId={ResourceId}, Action={Action}",
                organizationId,
                resourceId,
                action);
        }
    }

    private static string SanitizeAuditValue(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "n/a";
        }

        var trimmed = value.Trim();
        var sb = new StringBuilder(trimmed.Length);
        var previousWasWhitespace = false;

        foreach (var ch in trimmed)
        {
            if (char.IsControl(ch))
            {
                if (!previousWasWhitespace)
                {
                    sb.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                if (!previousWasWhitespace)
                {
                    sb.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            sb.Append(ch);
            previousWasWhitespace = false;

            if (sb.Length >= maxLength)
            {
                break;
            }
        }

        var normalized = sb.ToString().Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "n/a" : normalized;
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "***";
        }

        var atIndex = email.IndexOf('@');
        if (atIndex <= 1 || atIndex == email.Length - 1)
        {
            return "***";
        }

        var localPart = email[..atIndex];
        var domain = email[(atIndex + 1)..];
        var visible = localPart.Length <= 2 ? localPart[..1] : localPart[..2];

        return $"{visible}***@{domain}";
    }
}
