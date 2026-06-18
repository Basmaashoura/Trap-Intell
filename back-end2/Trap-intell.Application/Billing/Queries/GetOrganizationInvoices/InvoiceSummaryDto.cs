using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Queries.GetOrganizationInvoices;

public sealed record InvoiceSummaryDto(
    Guid Id,
    Guid SubscriptionId,
    string InvoiceNumber,
    InvoiceStatus Status,
    decimal BaseAmount,
    decimal OverageAmount,
    decimal TaxAmount,
    decimal Discount,
    decimal TotalAmount,
    string Currency,
    DateTime? IssueDate,
    DateTime? DueDate,
    DateTime CreatedAt,
    bool IsOverdue);
