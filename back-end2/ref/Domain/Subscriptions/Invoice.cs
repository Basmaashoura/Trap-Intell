using SharedKernel;

namespace Domain.Subscriptions;

/// <summary>
/// Represents a subscription invoice / billing record.
/// </summary>
public sealed class Invoice : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The subscription this invoice belongs to.
    /// </summary>
    public Guid SubscriptionId { get; private set; }

    /// <summary>
    /// The account being billed.
    /// </summary>
    public Guid AccountId { get; private set; }

    /// <summary>
    /// Human-readable invoice number (e.g., "INV-2025-0001").
    /// </summary>
    public string InvoiceNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Total amount due.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// ISO 4217 currency code.
    /// </summary>
    public string CurrencyCode { get; private set; } = "USD";

    /// <summary>
    /// Current status of this invoice.
    /// </summary>
    public InvoiceStatus Status { get; private set; }

    /// <summary>
    /// Short description (e.g., "Pro Plan - Monthly").
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// The billing period start date.
    /// </summary>
    public DateTime PeriodStart { get; private set; }

    /// <summary>
    /// The billing period end date.
    /// </summary>
    public DateTime PeriodEnd { get; private set; }

    /// <summary>
    /// When the invoice was issued.
    /// </summary>
    public DateTime IssuedAt { get; private set; }

    /// <summary>
    /// When payment is due.
    /// </summary>
    public DateTime DueDate { get; private set; }

    /// <summary>
    /// When the invoice was paid (null if unpaid).
    /// </summary>
    public DateTime? PaidAt { get; private set; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public Subscription Subscription { get; private set; } = null!;

    private Invoice()
    {
    }

    public static Invoice Create(
        Guid subscriptionId,
        Guid accountId,
        string invoiceNumber,
        decimal amount,
        string currencyCode,
        string description,
        DateTime periodStart,
        DateTime periodEnd,
        DateTime dueDate)
    {
        return new Invoice
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            AccountId = accountId,
            InvoiceNumber = invoiceNumber,
            Amount = amount,
            CurrencyCode = currencyCode,
            Status = InvoiceStatus.Issued,
            Description = description,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            IssuedAt = DateTime.UtcNow,
            DueDate = dueDate
        };
    }

    public void MarkAsPaid()
    {
        Status = InvoiceStatus.Paid;
        PaidAt = DateTime.UtcNow;
    }

    public void MarkAsOverdue()
    {
        Status = InvoiceStatus.Overdue;
    }

    public void Cancel()
    {
        Status = InvoiceStatus.Cancelled;
    }
}
