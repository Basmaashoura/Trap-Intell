using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Billing
{
    /// <summary>
    /// Domain events for the Billing domain.
    /// Immutable records representing important business events.
    /// </summary>

    // Invoice Events
    public record InvoiceCreatedEvent(
        Guid InvoiceId,
        Guid SubscriptionId,
        Guid OrganizationId,
        string InvoiceNumber,
        decimal TotalAmount,
        DateTime OccurredOn) : IDomainEvent;

    public record InvoiceIssuedEvent(
        Guid InvoiceId,
        Guid SubscriptionId,
        DateTime IssueDate,
        DateTime DueDate,
        decimal Amount,
        DateTime OccurredOn) : IDomainEvent;

    public record InvoicePaidEvent(
        Guid InvoiceId,
        Guid SubscriptionId,
        Guid PaymentId,
        decimal PaidAmount,
        DateTime OccurredOn) : IDomainEvent;

    public record InvoiceOverdueEvent(
        Guid InvoiceId,
        Guid SubscriptionId,
        decimal OverdueAmount,
        DateTime OccurredOn) : IDomainEvent;

    public record InvoiceCancelledEvent(
        Guid InvoiceId,
        Guid SubscriptionId,
        string Reason,
        DateTime OccurredOn) : IDomainEvent;

    public record InvoiceRefundedEvent(
        Guid InvoiceId,
        Guid SubscriptionId,
        decimal RefundAmount,
        string Reason,
        DateTime OccurredOn) : IDomainEvent;

    public record InvoiceLateFeeAppliedEvent(
        Guid InvoiceId,
        Guid SubscriptionId,
        decimal LateFeeAmount,
        DateTime OccurredOn) : IDomainEvent;

    // PaymentMethod Events
    public record PaymentMethodCreatedEvent(
        Guid PaymentMethodId,
        Guid OrganizationId,
        PaymentMethodType Type,
        DateTime OccurredOn) : IDomainEvent;

    public record PaymentMethodActivatedEvent(
        Guid PaymentMethodId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    public record PaymentMethodDeactivatedEvent(
        Guid PaymentMethodId,
        Guid OrganizationId,
        string Reason,
        DateTime OccurredOn) : IDomainEvent;

    public record PaymentMethodSuspendedEvent(
        Guid PaymentMethodId,
        Guid OrganizationId,
        string Reason,
        DateTime OccurredOn) : IDomainEvent;

    public record PaymentMethodExpiredEvent(
        Guid PaymentMethodId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    public record PaymentMethodSetAsDefaultEvent(
        Guid PaymentMethodId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    public record PaymentMethodUpdatedEvent(
        Guid PaymentMethodId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;
}
