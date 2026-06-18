using System.Security.Cryptography;
using System.Text;

namespace Trap_Intel.Application.Billing.Security;

internal static class PaymentTokenProtector
{
    private static readonly string[] ProviderReferencePrefixes =
    [
        "pm_", "tok_", "src_", "card_", "cus_", "ba_", "pi_", "seti_", "ch_", "tr_",
        "paypal_", "pay_", "adyen_", "braintree_"
    ];

    public static string? Protect(string? rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var normalized = rawToken.Trim();

        if (IsSha256Hex(normalized))
        {
            return normalized;
        }

        // Keep known provider references intact so the billing processor can execute real charges.
        if (IsLikelyProviderReference(normalized))
        {
            return normalized;
        }

        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }

    private static bool IsSha256Hex(string value)
    {
        return value.Length == 64 && value.All(Uri.IsHexDigit);
    }

    private static bool IsLikelyProviderReference(string value)
    {
        if (value.Contains(':'))
        {
            return true;
        }

        return ProviderReferencePrefixes.Any(prefix =>
            value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
