using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Infrastructure.Configuration;

namespace Trap_Intel.Infrastructure.Billing;

internal sealed class StripePaymentProcessor : IPaymentProcessor
{
    private static readonly Error NotConfiguredError = Error.Custom(
        "Billing.PaymentProcessorNotConfigured",
        "Stripe payment processor is not configured.");

    private static readonly Error MissingProviderTokenError = Error.Custom(
        "Billing.PaymentProviderTokenMissing",
        "Payment method is missing a provider token reference.");

    private static readonly Error ProtectedTokenError = Error.Custom(
        "Billing.PaymentProviderTokenProtected",
        "Stored payment token is protected and cannot be charged. Re-link the payment method with a provider reference token (for example: pm_...).");

    private static readonly Error MissingProviderReferenceError = Error.Custom(
        "Billing.PaymentProviderReferenceMissing",
        "Payment provider reference for this payment is not available. Refund cannot be processed automatically.");

    private static readonly HashSet<string> ZeroDecimalCurrencies =
    [
        "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga", "pyg",
        "rwf", "ugx", "vnd", "vuv", "xaf", "xof", "xpf"
    ];

    private static readonly ConcurrentDictionary<Guid, string> PaymentReferenceIndex = new();
    private const string ProviderReferenceNotePrefix = "PaymentProviderReference:stripe:";

    private readonly HttpClient _httpClient;
    private readonly PaymentGatewaySettings _settings;
    private readonly ILogger<StripePaymentProcessor> _logger;
    private readonly IInvoiceRepository _invoiceRepository;

    public StripePaymentProcessor(
        HttpClient httpClient,
        IOptions<PaymentGatewaySettings> settings,
        ILogger<StripePaymentProcessor> logger,
        IInvoiceRepository invoiceRepository)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _invoiceRepository = invoiceRepository;
    }

    public Task<Result<Guid>> ChargeAsync(
        PaymentMethod paymentMethod,
        decimal amount,
        string currency,
        string invoiceNumber,
        string description,
        CancellationToken cancellationToken = default)
    {
        return ChargeCoreAsync(
            paymentMethod,
            amount,
            currency,
            invoiceNumber,
            description,
            idempotencyKey: null,
            cancellationToken);
    }

    public Task<Result<Guid>> ChargeAsync(
        PaymentMethod paymentMethod,
        decimal amount,
        string currency,
        string invoiceNumber,
        string description,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return ChargeCoreAsync(
            paymentMethod,
            amount,
            currency,
            invoiceNumber,
            description,
            idempotencyKey,
            cancellationToken);
    }

    private async Task<Result<Guid>> ChargeCoreAsync(
        PaymentMethod paymentMethod,
        decimal amount,
        string currency,
        string invoiceNumber,
        string description,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!_settings.IsStripeConfigured)
        {
            return Result.Failure<Guid>(NotConfiguredError);
        }

        if (amount <= 0)
        {
            return Result.Failure<Guid>(Error.Custom("Billing.InvalidAmount", "Charge amount must be greater than zero."));
        }

        var providerToken = paymentMethod.Details.Token?.Trim();
        if (string.IsNullOrWhiteSpace(providerToken))
        {
            return Result.Failure<Guid>(MissingProviderTokenError);
        }

        if (LooksLikeSha256Hash(providerToken))
        {
            return Result.Failure<Guid>(ProtectedTokenError);
        }

        var normalizedIdempotencyKey = BillingIdempotency.NormalizeKey(idempotencyKey);
        var normalizedCurrency = NormalizeCurrency(currency);
        var minorAmount = ToMinorUnits(amount, normalizedCurrency);

        var form = new Dictionary<string, string>
        {
            ["amount"] = minorAmount.ToString(CultureInfo.InvariantCulture),
            ["currency"] = normalizedCurrency,
            ["payment_method"] = providerToken,
            ["confirm"] = "true",
            ["off_session"] = "true",
            ["description"] = description,
            ["metadata[invoice_number]"] = invoiceNumber,
            ["metadata[organization_id]"] = paymentMethod.OrganizationId.ToString(),
            ["metadata[payment_method_id]"] = paymentMethod.Id.ToString()
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "payment_intents")
        {
            Content = new FormUrlEncodedContent(form)
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.StripeSecretKey);
        if (normalizedIdempotencyKey is not null)
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", normalizedIdempotencyKey);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<Guid>(BuildStripeError("Billing.PaymentChargeFailed", body, response.ReasonPhrase));
        }

        using var json = JsonDocument.Parse(body);
        var status = ReadString(json.RootElement, "status");
        if (!string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(status, "processing", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(status, "requires_capture", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<Guid>(Error.Custom(
                "Billing.PaymentChargeFailed",
                $"Stripe returned status '{status ?? "unknown"}' for invoice {invoiceNumber}."));
        }

        var providerPaymentReference = ReadString(json.RootElement, "id");
        if (string.IsNullOrWhiteSpace(providerPaymentReference))
        {
            return Result.Failure<Guid>(Error.Custom(
                "Billing.PaymentChargeFailed",
                "Stripe response did not include a payment intent identifier."));
        }

        var internalPaymentId = normalizedIdempotencyKey is null
            ? Guid.NewGuid()
            : BillingIdempotency.CreatePaymentOperationId(invoiceNumber, normalizedIdempotencyKey);

        PaymentReferenceIndex[internalPaymentId] = providerPaymentReference;

        await TryPersistProviderReferenceAsync(
            internalPaymentId,
            invoiceNumber,
            providerPaymentReference,
            cancellationToken);

        _logger.LogInformation(
            "Stripe charge created for invoice {InvoiceNumber}. InternalPaymentId={InternalPaymentId}, ProviderReference={ProviderReference}",
            invoiceNumber,
            internalPaymentId,
            providerPaymentReference);

        return Result.Success(internalPaymentId);
    }

    public Task<Result<Guid>> RefundAsync(
        Guid paymentId,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return RefundCoreAsync(paymentId, amount, reason, idempotencyKey: null, cancellationToken);
    }

    public Task<Result<Guid>> RefundAsync(
        Guid paymentId,
        decimal amount,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return RefundCoreAsync(paymentId, amount, reason, idempotencyKey, cancellationToken);
    }

    private async Task<Result<Guid>> RefundCoreAsync(
        Guid paymentId,
        decimal amount,
        string reason,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!_settings.IsStripeConfigured)
        {
            return Result.Failure<Guid>(NotConfiguredError);
        }

        var normalizedIdempotencyKey = BillingIdempotency.NormalizeKey(idempotencyKey);

        if (!PaymentReferenceIndex.TryGetValue(paymentId, out var providerPaymentReference))
        {
            providerPaymentReference = await TryRestoreProviderReferenceAsync(paymentId, cancellationToken);
            if (string.IsNullOrWhiteSpace(providerPaymentReference))
            {
                return Result.Failure<Guid>(MissingProviderReferenceError);
            }
        }

        var form = new Dictionary<string, string>
        {
            ["payment_intent"] = providerPaymentReference
        };

        if (amount > 0)
        {
            // Stripe requires minor currency units for amount. Refund uses the original charge currency internally.
            // We assume USD equivalent for partial refund fallback because the domain currently omits refund currency.
            var refundMinorUnits = ToMinorUnits(amount, "usd");
            form["amount"] = refundMinorUnits.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            form["metadata[reason]"] = reason.Trim();
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "refunds")
        {
            Content = new FormUrlEncodedContent(form)
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.StripeSecretKey);
        if (normalizedIdempotencyKey is not null)
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", normalizedIdempotencyKey);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<Guid>(BuildStripeError("Billing.RefundFailed", body, response.ReasonPhrase));
        }

        var refundId = normalizedIdempotencyKey is null
            ? Guid.NewGuid()
            : BillingIdempotency.CreateRefundOperationId(paymentId, normalizedIdempotencyKey);

        return Result.Success(refundId);
    }

    public async Task<Result<bool>> VerifyAsync(
        PaymentMethod paymentMethod,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsStripeConfigured)
        {
            return Result.Failure<bool>(NotConfiguredError);
        }

        var providerToken = paymentMethod.Details.Token?.Trim();
        if (string.IsNullOrWhiteSpace(providerToken))
        {
            return Result.Failure<bool>(MissingProviderTokenError);
        }

        if (LooksLikeSha256Hash(providerToken))
        {
            return Result.Failure<bool>(ProtectedTokenError);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"payment_methods/{Uri.EscapeDataString(providerToken)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.StripeSecretKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return Result.Success(true);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result.Failure<bool>(BuildStripeError("Billing.PaymentMethodVerificationFailed", body, response.ReasonPhrase));
    }

    private static string NormalizeCurrency(string currency)
    {
        var normalized = string.IsNullOrWhiteSpace(currency) ? "usd" : currency.Trim().ToLowerInvariant();
        return normalized.Length == 3 ? normalized : "usd";
    }

    private static long ToMinorUnits(decimal amount, string currency)
    {
        if (ZeroDecimalCurrencies.Contains(currency))
        {
            return (long)Math.Round(amount, MidpointRounding.AwayFromZero);
        }

        return (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static Error BuildStripeError(string code, string responseBody, string? fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                var message = ReadString(errorElement, "message");
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return Error.Custom(code, message);
                }
            }
        }
        catch
        {
            // If parsing fails we still return a safe generic error.
        }

        return Error.Custom(code, fallback ?? "Stripe request failed.");
    }

    private static bool LooksLikeSha256Hash(string token)
    {
        return token.Length == 64 && token.All(Uri.IsHexDigit);
    }

    private async Task TryPersistProviderReferenceAsync(
        Guid internalPaymentId,
        string invoiceNumber,
        string providerPaymentReference,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByInvoiceNumberAsync(invoiceNumber, cancellationToken);
        if (invoice is null)
        {
            _logger.LogWarning(
                "Could not persist provider reference because invoice {InvoiceNumber} was not found.",
                invoiceNumber);
            return;
        }

        var note = BuildProviderReferenceNote(internalPaymentId, providerPaymentReference);
        if (invoice.Notes.Contains(note, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        var addNoteResult = invoice.AddNote(note);
        if (addNoteResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to persist Stripe provider reference note for invoice {InvoiceNumber}. Error={ErrorCode}",
                invoiceNumber,
                addNoteResult.Errors.FirstOrDefault()?.Code);
            return;
        }

        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
    }

    private async Task<string?> TryRestoreProviderReferenceAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByPaymentIdAsync(paymentId, cancellationToken);
        if (invoice is null)
        {
            return null;
        }

        var providerPaymentReference = ExtractProviderReference(invoice.Notes, paymentId);
        if (string.IsNullOrWhiteSpace(providerPaymentReference))
        {
            return null;
        }

        PaymentReferenceIndex[paymentId] = providerPaymentReference;
        return providerPaymentReference;
    }

    private static string BuildProviderReferenceNote(Guid paymentId, string providerPaymentReference)
    {
        return $"{ProviderReferenceNotePrefix}{paymentId:N}:{providerPaymentReference}";
    }

    private static string? ExtractProviderReference(IReadOnlyList<string> notes, Guid paymentId)
    {
        var expectedPrefix = $"{ProviderReferenceNotePrefix}{paymentId:N}:";

        var note = notes.FirstOrDefault(item =>
            item.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(note) || note.Length <= expectedPrefix.Length)
        {
            return null;
        }

        return note[expectedPrefix.Length..].Trim();
    }
}