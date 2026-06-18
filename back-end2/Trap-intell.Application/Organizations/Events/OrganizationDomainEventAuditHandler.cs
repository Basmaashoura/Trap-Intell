using MediatR;
using Microsoft.Extensions.Logging;
using System.Text;
using Trap_Intel.Application.Abstractions.Auditing;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Domain.Organizations.Events;

namespace Trap_Intel.Application.Organizations.Events;

internal sealed class OrganizationDomainEventAuditHandler :
    INotificationHandler<OrganizationCreatedEvent>,
    INotificationHandler<OrganizationApprovedEvent>,
    INotificationHandler<OrganizationRejectedEvent>,
    INotificationHandler<OrganizationInfoUpdatedEvent>,
    INotificationHandler<OrganizationActivatedEvent>,
    INotificationHandler<OrganizationSuspendedEvent>,
    INotificationHandler<OrganizationDeactivatedEvent>,
    INotificationHandler<OrganizationDeletedEvent>,
    INotificationHandler<AddressAddedEvent>,
    INotificationHandler<AddressRemovedEvent>
{
    private const int MaxAuditReasonLength = 1000;
    private const int MaxAuditFieldLength = 160;

    private readonly IAuditService _auditService;
    private readonly ILogger<OrganizationDomainEventAuditHandler> _logger;

    public OrganizationDomainEventAuditHandler(
        IAuditService auditService,
        ILogger<OrganizationDomainEventAuditHandler> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public Task Handle(OrganizationCreatedEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.OrganizationId,
            AuditAction.Create,
            AuditSeverity.Info,
            $"Organization '{SanitizeAuditValue(notification.Name, MaxAuditFieldLength)}' created in industry '{SanitizeAuditValue(notification.Industry, MaxAuditFieldLength)}'.",
            cancellationToken);
    }

    public Task Handle(OrganizationApprovedEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.OrganizationId,
            AuditAction.Approve,
            AuditSeverity.Info,
            "Organization was approved and moved to active status.",
            cancellationToken);
    }

    public Task Handle(OrganizationRejectedEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.OrganizationId,
            AuditAction.Reject,
            AuditSeverity.Warning,
            $"Organization was rejected. Reason: {SanitizeAuditValue(notification.Reason, MaxAuditFieldLength)}",
            cancellationToken);
    }

    public Task Handle(OrganizationInfoUpdatedEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.OrganizationId,
            AuditAction.Update,
            AuditSeverity.Info,
            $"Organization information updated. Fields: {SanitizeAuditValue(notification.ChangedFields, MaxAuditFieldLength)}.",
            cancellationToken);
    }

    public Task Handle(OrganizationActivatedEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.OrganizationId,
            AuditAction.Activate,
            AuditSeverity.Info,
            "Organization activated.",
            cancellationToken);
    }

    public Task Handle(OrganizationSuspendedEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.OrganizationId,
            AuditAction.Suspend,
            AuditSeverity.Warning,
            "Organization suspended.",
            cancellationToken);
    }

    public Task Handle(OrganizationDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.OrganizationId,
            AuditAction.Deactivate,
            AuditSeverity.Warning,
            "Organization deactivated.",
            cancellationToken);
    }

    public Task Handle(OrganizationDeletedEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.OrganizationId,
            AuditAction.Delete,
            AuditSeverity.Warning,
            $"Organization deleted. Reason: {SanitizeAuditValue(notification.Reason, MaxAuditFieldLength)}",
            cancellationToken);
    }

    public Task Handle(AddressAddedEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.OrganizationId,
            AuditAction.Update,
            AuditSeverity.Info,
            $"Address added (type: {SanitizeAuditValue(notification.AddressType, MaxAuditFieldLength)}) in {SanitizeAuditValue(notification.City, MaxAuditFieldLength)}, {SanitizeAuditValue(notification.Country, MaxAuditFieldLength)}.",
            cancellationToken);
    }

    public Task Handle(AddressRemovedEvent notification, CancellationToken cancellationToken)
    {
        return RecordAsync(
            notification.OrganizationId,
            notification.OrganizationId,
            AuditAction.Update,
            AuditSeverity.Info,
            $"Address removed (city: {SanitizeAuditValue(notification.City, MaxAuditFieldLength)}).",
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
                "Skipping organization audit event due to invalid ids. OrgId={OrgId}, ResourceId={ResourceId}, Action={Action}",
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
                "Failed to persist organization audit record. OrgId={OrgId}, Action={Action}",
                organizationId,
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
}
