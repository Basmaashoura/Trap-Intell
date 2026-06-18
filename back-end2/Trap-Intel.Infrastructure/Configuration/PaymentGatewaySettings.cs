namespace Trap_Intel.Infrastructure.Configuration;

public sealed class PaymentGatewaySettings
{
    public const string SectionName = "PaymentGateway";

    public string Provider { get; init; } = "Stripe";
    public string StripeSecretKey { get; init; } = string.Empty;
    public string StripeApiBaseUrl { get; init; } = "https://api.stripe.com/v1/";

    public bool IsStripeConfigured =>
        Provider.Equals("Stripe", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(StripeSecretKey);
}