using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Alerts.Notifications;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Alerts.Enums;
using Trap_Intel.Domain.Alerts.ValueObjects;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications.Enums;

namespace Trap_Intel.Application.Billing.Events;

internal sealed class InvoiceDomainEventAlertHandler :
    INotificationHandler<InvoiceCancelledEvent>,
    INotificationHandler<InvoiceRefundedEvent>,
    INotificationHandler<InvoiceOverdueEvent>,
    INotificationHandler<InvoiceLateFeeAppliedEvent>
{
    private readonly IAlertRepository _alertRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InvoiceDomainEventAlertHandler> _logger;

    public InvoiceDomainEventAlertHandler(
        IAlertRepository alertRepository,
        IInvoiceRepository invoiceRepository,
        IUserRepository userRepository,
        INotificationDispatcher notificationDispatcher,
        IUnitOfWork unitOfWork,
        ILogger<InvoiceDomainEventAlertHandler> logger)
    {
        _alertRepository = alertRepository;
        _invoiceRepository = invoiceRepository;
        _userRepository = userRepository;
        _notificationDispatcher = notificationDispatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(InvoiceOverdueEvent notification, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(notification.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return;
        }

        if (await HasAlertForInvoiceAsync(
            invoice.OrganizationId,
            notification.InvoiceId,
            AlertType.SubscriptionExpiring,
            sourceType: "Invoice",
            cancellationToken))
        {
            return;
        }

        var source = new AlertSource
        {
            SourceType = "Invoice",
            SourceId = invoice.Id,
            SourceName = invoice.InvoiceNumber.Value
        };

        var alertResult = Alert.Create(
            invoice.OrganizationId,
            AlertType.SubscriptionExpiring,
            AlertSeverity.High,
            title: $"Invoice overdue: {invoice.InvoiceNumber.Value}",
            description: $"Invoice {invoice.InvoiceNumber.Value} is overdue with outstanding amount {notification.OverdueAmount:N2} {invoice.Amount.Currency}.",
            source: source,
            expiresIn: TimeSpan.FromDays(30));

        if (alertResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create invoice overdue alert. InvoiceId={InvoiceId}, Errors={Errors}",
                notification.InvoiceId,
                string.Join(',', alertResult.Errors.Select(error => error.Code)));
            return;
        }

        await PersistAndNotifyAsync(
            alertResult.Value,
            $"Invoice {invoice.InvoiceNumber.Value} is overdue and needs immediate action.",
            cancellationToken);
    }

    public async Task Handle(InvoiceLateFeeAppliedEvent notification, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(notification.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return;
        }

        if (await HasAlertForInvoiceAsync(
            invoice.OrganizationId,
            notification.InvoiceId,
            AlertType.Custom,
            sourceType: "InvoiceLateFee",
            cancellationToken))
        {
            return;
        }

        var source = new AlertSource
        {
            SourceType = "InvoiceLateFee",
            SourceId = invoice.Id,
            SourceName = invoice.InvoiceNumber.Value
        };

        var alertResult = Alert.Create(
            invoice.OrganizationId,
            AlertType.Custom,
            AlertSeverity.Medium,
            title: $"Late fee applied: {invoice.InvoiceNumber.Value}",
            description: $"A late fee of {notification.LateFeeAmount:N2} {invoice.Amount.Currency} was applied to invoice {invoice.InvoiceNumber.Value}.",
            source: source,
            expiresIn: TimeSpan.FromDays(14));

        if (alertResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create late fee alert. InvoiceId={InvoiceId}, Errors={Errors}",
                notification.InvoiceId,
                string.Join(',', alertResult.Errors.Select(error => error.Code)));
            return;
        }

        await PersistAndNotifyAsync(
            alertResult.Value,
            $"Late fee was applied to invoice {invoice.InvoiceNumber.Value}.",
            cancellationToken);
    }

    public async Task Handle(InvoiceCancelledEvent notification, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(notification.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return;
        }

        if (await HasAlertForInvoiceAsync(
                invoice.OrganizationId,
                notification.InvoiceId,
                AlertType.Custom,
                sourceType: "InvoiceCancellation",
                cancellationToken))
        {
            return;
        }

        var source = new AlertSource
        {
            SourceType = "InvoiceCancellation",
            SourceId = invoice.Id,
            SourceName = invoice.InvoiceNumber.Value
        };

        var alertResult = Alert.Create(
            invoice.OrganizationId,
            AlertType.Custom,
            AlertSeverity.Low,
            title: $"Invoice cancelled: {invoice.InvoiceNumber.Value}",
            description: $"Invoice {invoice.InvoiceNumber.Value} was cancelled. Reason: {notification.Reason}.",
            source: source,
            expiresIn: TimeSpan.FromDays(7));

        if (alertResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create cancellation alert. InvoiceId={InvoiceId}, Errors={Errors}",
                notification.InvoiceId,
                string.Join(',', alertResult.Errors.Select(error => error.Code)));
            return;
        }

        await PersistAndNotifyAsync(
            alertResult.Value,
            $"Invoice {invoice.InvoiceNumber.Value} was cancelled.",
            cancellationToken);
    }

    public async Task Handle(InvoiceRefundedEvent notification, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(notification.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return;
        }

        if (await HasAlertForInvoiceAsync(
                invoice.OrganizationId,
                notification.InvoiceId,
                AlertType.Custom,
                sourceType: "InvoiceRefund",
                cancellationToken))
        {
            return;
        }

        var source = new AlertSource
        {
            SourceType = "InvoiceRefund",
            SourceId = invoice.Id,
            SourceName = invoice.InvoiceNumber.Value
        };

        var alertResult = Alert.Create(
            invoice.OrganizationId,
            AlertType.Custom,
            AlertSeverity.Medium,
            title: $"Invoice refunded: {invoice.InvoiceNumber.Value}",
            description: $"Invoice {invoice.InvoiceNumber.Value} was refunded by {notification.RefundAmount:N2} {invoice.Amount.Currency}. Reason: {notification.Reason}.",
            source: source,
            expiresIn: TimeSpan.FromDays(14));

        if (alertResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create refund alert. InvoiceId={InvoiceId}, Errors={Errors}",
                notification.InvoiceId,
                string.Join(',', alertResult.Errors.Select(error => error.Code)));
            return;
        }

        await PersistAndNotifyAsync(
            alertResult.Value,
            $"Invoice {invoice.InvoiceNumber.Value} was refunded.",
            cancellationToken);
    }

    private async Task<bool> HasAlertForInvoiceAsync(
        Guid organizationId,
        Guid invoiceId,
        AlertType alertType,
        string? sourceType,
        CancellationToken cancellationToken)
    {
        var alerts = await _alertRepository.GetBySourceIdAsync(invoiceId, cancellationToken);

        return alerts.Any(alert =>
            alert.OrganizationId == organizationId &&
            alert.AlertType == alertType &&
            (sourceType is null || string.Equals(alert.Source.SourceType, sourceType, StringComparison.OrdinalIgnoreCase)) &&
            (alert.Status == AlertStatus.New ||
             alert.Status == AlertStatus.Acknowledged ||
             alert.Status == AlertStatus.InProgress));
    }

    private async Task PersistAndNotifyAsync(Alert alert, string notificationMessage, CancellationToken cancellationToken)
    {
        await _alertRepository.AddAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await AlertNotificationPublisher.PublishAsync(
                alert,
                AlertNotificationType.AlertCreated,
                notificationMessage,
                _userRepository,
                _notificationDispatcher,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish invoice alert notification for alert {AlertId}", alert.Id);
        }
    }
}
