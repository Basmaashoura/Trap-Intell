using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications;
using Trap_Intel.Domain.Notifications.Enums;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Application.Billing.Events;

internal sealed class InvoiceDomainEventNotificationHandler :
    INotificationHandler<InvoiceCreatedEvent>,
    INotificationHandler<InvoiceIssuedEvent>,
    INotificationHandler<InvoicePaidEvent>,
    INotificationHandler<InvoiceCancelledEvent>,
    INotificationHandler<InvoiceRefundedEvent>,
    INotificationHandler<InvoiceOverdueEvent>,
    INotificationHandler<InvoiceLateFeeAppliedEvent>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<InvoiceDomainEventNotificationHandler> _logger;

    public InvoiceDomainEventNotificationHandler(
        IInvoiceRepository invoiceRepository,
        IUserRepository userRepository,
        INotificationDispatcher notificationDispatcher,
        ILogger<InvoiceDomainEventNotificationHandler> logger)
    {
        _invoiceRepository = invoiceRepository;
        _userRepository = userRepository;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    public async Task Handle(InvoiceCreatedEvent notification, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(notification.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return;
        }

        await DispatchToBillingAdminsAsync(
            organizationId: invoice.OrganizationId,
            type: "InvoiceCreated",
            title: $"Invoice created: {invoice.InvoiceNumber.Value}",
            message: $"Draft invoice {invoice.InvoiceNumber.Value} was created for {invoice.Amount.TotalAmount:N2} {invoice.Amount.Currency}.",
            priority: NotificationPriority.Normal,
            invoiceId: invoice.Id,
            cancellationToken: cancellationToken);
    }

    public async Task Handle(InvoiceIssuedEvent notification, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(notification.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return;
        }

        await DispatchToBillingAdminsAsync(
            organizationId: invoice.OrganizationId,
            type: "InvoiceIssued",
            title: $"Invoice issued: {invoice.InvoiceNumber.Value}",
            message: $"Invoice {invoice.InvoiceNumber.Value} was issued for {invoice.Amount.TotalAmount:N2} {invoice.Amount.Currency}. Due date: {invoice.DueDate:yyyy-MM-dd}.",
            priority: NotificationPriority.Normal,
            invoiceId: invoice.Id,
            cancellationToken: cancellationToken);
    }

    public async Task Handle(InvoicePaidEvent notification, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(notification.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return;
        }

        await DispatchToBillingAdminsAsync(
            organizationId: invoice.OrganizationId,
            type: "InvoicePaid",
            title: $"Invoice paid: {invoice.InvoiceNumber.Value}",
            message: $"Invoice {invoice.InvoiceNumber.Value} was paid successfully. Payment ID: {notification.PaymentId}.",
            priority: NotificationPriority.Normal,
            invoiceId: invoice.Id,
            cancellationToken: cancellationToken);
    }

    public async Task Handle(InvoiceCancelledEvent notification, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(notification.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return;
        }

        await DispatchToBillingAdminsAsync(
            organizationId: invoice.OrganizationId,
            type: "InvoiceCancelled",
            title: $"Invoice cancelled: {invoice.InvoiceNumber.Value}",
            message: $"Invoice {invoice.InvoiceNumber.Value} was cancelled. Reason: {notification.Reason}.",
            priority: NotificationPriority.High,
            invoiceId: invoice.Id,
            cancellationToken: cancellationToken);
    }

    public async Task Handle(InvoiceRefundedEvent notification, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(notification.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return;
        }

        await DispatchToBillingAdminsAsync(
            organizationId: invoice.OrganizationId,
            type: "InvoiceRefunded",
            title: $"Invoice refunded: {invoice.InvoiceNumber.Value}",
            message: $"Invoice {invoice.InvoiceNumber.Value} was refunded by {notification.RefundAmount:N2} {invoice.Amount.Currency}. Reason: {notification.Reason}.",
            priority: NotificationPriority.High,
            invoiceId: invoice.Id,
            cancellationToken: cancellationToken);
    }

    public async Task Handle(InvoiceOverdueEvent notification, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(notification.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return;
        }

        await DispatchToBillingAdminsAsync(
            organizationId: invoice.OrganizationId,
            type: "InvoiceOverdue",
            title: $"Invoice overdue: {invoice.InvoiceNumber.Value}",
            message: $"Invoice {invoice.InvoiceNumber.Value} is overdue with amount {notification.OverdueAmount:N2} {invoice.Amount.Currency}. Immediate follow-up is required.",
            priority: NotificationPriority.High,
            invoiceId: invoice.Id,
            cancellationToken: cancellationToken);
    }

    public async Task Handle(InvoiceLateFeeAppliedEvent notification, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(notification.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return;
        }

        await DispatchToBillingAdminsAsync(
            organizationId: invoice.OrganizationId,
            type: "InvoiceLateFeeApplied",
            title: $"Late fee applied: {invoice.InvoiceNumber.Value}",
            message: $"A late fee of {notification.LateFeeAmount:N2} {invoice.Amount.Currency} was applied to invoice {invoice.InvoiceNumber.Value}.",
            priority: NotificationPriority.High,
            invoiceId: invoice.Id,
            cancellationToken: cancellationToken);
    }

    private async Task DispatchToBillingAdminsAsync(
        Guid organizationId,
        string type,
        string title,
        string message,
        NotificationPriority priority,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var orgAdmins = await _userRepository.GetByRoleAsync(organizationId, SystemRoles.OrganizationAdminId, cancellationToken);
        var superAdmins = await _userRepository.GetByRoleAsync(organizationId, SystemRoles.SuperAdminId, cancellationToken);

        var recipients = orgAdmins
            .Concat(superAdmins)
            .DistinctBy(user => user.Id)
            .ToList();

        foreach (var recipient in recipients)
        {
            var notificationResult = Notification.Create(
                userId: recipient.Id,
                type: type,
                title: title,
                message: message,
                category: NotificationCategory.Billing,
                priority: priority,
                linkUri: $"/invoices/{invoiceId}",
                relatedEntityId: invoiceId.ToString());

            if (notificationResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to build billing notification for user {UserId}. Type={Type}",
                    recipient.Id,
                    type);
                continue;
            }

            await _notificationDispatcher.DispatchAsync(notificationResult.Value, cancellationToken);
        }
    }
}
