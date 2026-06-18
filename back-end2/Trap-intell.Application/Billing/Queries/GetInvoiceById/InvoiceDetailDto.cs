using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Queries.GetInvoiceById;

public sealed record InvoiceDetailDto(
    Guid Id,
    Guid SubscriptionId,
    Guid OrganizationId,
    string InvoiceNumber,
    InvoiceStatus Status,
    DateTime BillingPeriodStart,
    DateTime BillingPeriodEnd,
    decimal BaseAmount,
    decimal OverageAmount,
    decimal TaxAmount,
    decimal Discount,
    decimal TotalAmount,
    string Currency,
    int HoneypotsUsed,
    decimal StorageUsedGb,
    decimal UsageOverageCharges,
    decimal TaxRate,
    string? TaxId,
    DateTime? IssueDate,
    DateTime? DueDate,
    Guid? PaymentId,
    IReadOnlyList<string> Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsOverdue);
