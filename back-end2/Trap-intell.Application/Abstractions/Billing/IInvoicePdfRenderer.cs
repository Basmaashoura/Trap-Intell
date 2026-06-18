using System;
using System.Collections.Generic;

namespace Trap_Intel.Application.Abstractions.Billing;

public sealed record InvoicePdfPayload(
    Guid InvoiceId,
    string InvoiceNumber,
    string OrganizationName,
    DateTime BillingPeriodStart,
    DateTime BillingPeriodEnd,
    DateTime? IssueDate,
    DateTime? DueDate,
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
    IReadOnlyList<string> Notes,
    bool IsOverdue);

public interface IInvoicePdfRenderer
{
    Task<byte[]> RenderAsync(InvoicePdfPayload payload, CancellationToken cancellationToken = default);
}
