namespace Trap_Intel.Api.Endpoints.PaymentMethods.Models;

public sealed record CreatePaymentMethodRequest(
    string Type,
    string? LastFourDigits,
    string? CardBrand,
    string? PaymentProcessor,
    string? Token,
    DateTime? ExpiresAt,
    string? BillingContactEmail,
    bool IsDefault = false);

public sealed record UpdatePaymentMethodRequest(
    string? LastFourDigits,
    string? CardBrand,
    string? PaymentProcessor,
    string? Token,
    DateTime? ExpiresAt,
    string? BillingContactEmail);

public sealed record DeactivatePaymentMethodRequest(
    string? Reason);
