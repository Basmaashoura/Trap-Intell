using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Tests.Billing.Support;

internal static class InvoiceBillingTestEntityFactory
{
    public static Invoice CreateIssuedInvoiceForPeriod(
        Guid organizationId,
        Guid subscriptionId,
        DateTime periodStart,
        DateTime periodEnd,
        string invoiceNumber)
    {
        var invoiceNumberResult = InvoiceNumber.Create(invoiceNumber);
        if (invoiceNumberResult.IsFailure)
        {
            throw new InvalidOperationException("Failed to create invoice number for test fixture.");
        }

        return Invoice.Reconstruct(
            id: Guid.NewGuid(),
            subscriptionId: subscriptionId,
            organizationId: organizationId,
            invoiceNumber: invoiceNumberResult.Value,
            status: InvoiceStatus.Issued,
            billingPeriod: new BillingPeriod(periodStart, periodEnd),
            amount: new InvoiceAmount(100m, 0m, 5m, 0m, "USD"),
            usageDetails: new UsageDetails(3, 10m, 0m),
            taxInfo: new TaxInfo("TAX-1", 0.05m),
            issueDate: DateTime.UtcNow.AddDays(-2),
            dueDate: DateTime.UtcNow.AddDays(10),
            paymentId: null,
            createdAt: DateTime.UtcNow.AddDays(-2),
            updatedAt: DateTime.UtcNow.AddDays(-2),
            notes: new List<string>());
    }

    public static PaymentMethod CreateUsablePaymentMethod(Guid organizationId)
    {
        var details = new PaymentMethodDetails(
            lastFourDigits: "4242",
            cardBrand: "Visa",
            paymentProcessor: "Stripe",
            token: "pm_test_renewal",
            expiresAt: DateTime.UtcNow.AddYears(2),
            billingContactEmail: "billing@trapintel.local");

        var createResult = PaymentMethod.Create(
            organizationId,
            PaymentMethodType.CreditCard,
            details);

        if (createResult.IsFailure)
        {
            throw new InvalidOperationException("Failed to create payment method test fixture.");
        }

        return createResult.Value;
    }
}
