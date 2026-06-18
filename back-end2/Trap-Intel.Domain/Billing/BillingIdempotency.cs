using System.Security.Cryptography;
using System.Text;

namespace Trap_Intel.Domain.Billing;

public static class BillingIdempotency
{
    public const int MaxKeyLength = 200;

    public static string? NormalizeKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        var normalized = idempotencyKey.Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    public static Guid CreatePaymentOperationId(string invoiceNumber, string idempotencyKey)
    {
        var normalizedInvoiceNumber = string.IsNullOrWhiteSpace(invoiceNumber)
            ? "unknown-invoice"
            : invoiceNumber.Trim().ToUpperInvariant();

        return CreateDeterministicGuid("payment-charge", normalizedInvoiceNumber, idempotencyKey);
    }

    public static Guid CreateRefundOperationId(Guid paymentId, string idempotencyKey)
    {
        return CreateDeterministicGuid("payment-refund", paymentId.ToString("N"), idempotencyKey);
    }

    private static Guid CreateDeterministicGuid(string operation, string scope, string idempotencyKey)
    {
        var normalizedKey = NormalizeKey(idempotencyKey)
            ?? throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));

        var payload = $"{operation}|{scope}|{normalizedKey}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));

        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, guidBytes.Length).CopyTo(guidBytes);

        return new Guid(guidBytes);
    }
}