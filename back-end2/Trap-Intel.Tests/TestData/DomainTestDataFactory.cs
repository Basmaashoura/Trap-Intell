using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Tests.TestData;

internal static class DomainTestDataFactory
{
    public static Subscription CreateSubscription(
        Guid organizationId,
        Guid planId,
        BillingCycle billingCycle = BillingCycle.Monthly)
    {
        var result = Subscription.Create(
            organizationId,
            planId,
            new SubscriptionPeriod(
                DateTime.UtcNow.AddDays(-7),
                DateTime.UtcNow.AddMonths(1),
                DateTime.UtcNow.AddMonths(1)),
            billingCycle,
            new BillingInfo(billingCycle, 99m));

        if (result.IsFailure)
        {
            throw new InvalidOperationException("Failed to create subscription test data.");
        }

        return result.Value;
    }

    public static Invoice CreateInvoice(
        Guid organizationId,
        Guid subscriptionId,
        string invoiceNumber,
        InvoiceStatus status,
        DateTime? issueDate = null,
        DateTime? dueDate = null,
        Guid? paymentId = null)
    {
        var invoiceNumberResult = InvoiceNumber.Create(invoiceNumber);
        if (invoiceNumberResult.IsFailure)
        {
            throw new InvalidOperationException("Failed to create invoice number test data.");
        }

        var now = DateTime.UtcNow;

        return Invoice.Reconstruct(
            Guid.NewGuid(),
            subscriptionId,
            organizationId,
            invoiceNumberResult.Value,
            status,
            new BillingPeriod(now.AddDays(-30), now.AddDays(-1)),
            new InvoiceAmount(100m, 20m, 5m, 0m, "USD"),
            new UsageDetails(5, 12m, 20m),
            new TaxInfo("TAX-1", 0.05m),
            issueDate,
            dueDate,
            paymentId,
            now.AddDays(-30),
            now.AddDays(-1),
            new List<string>());
    }
}
