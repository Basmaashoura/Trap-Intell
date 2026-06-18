namespace Domain.Subscriptions;

/// <summary>
/// Status of a subscription invoice.
/// </summary>
public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5
}
