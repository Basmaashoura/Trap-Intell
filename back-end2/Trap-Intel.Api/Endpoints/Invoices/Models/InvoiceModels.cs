namespace Trap_Intel.Api.Endpoints.Invoices.Models;

public sealed record IssueInvoiceRequest(
    int DaysDue = 30);

public sealed record ProcessInvoicePaymentRequest(
    Guid? PaymentMethodId = null,
    string? IdempotencyKey = null);

public sealed record CancelInvoiceRequest(
    string? Reason);

public sealed record RefundInvoiceRequest(
    decimal RefundAmount,
    string Reason,
    string? IdempotencyKey = null);
