using System;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Domain.Billing
{
    /// <summary>
    /// Domain service that coordinates invoice creation from subscription data.
    /// Handles the complex workflow of creating invoices with calculated amounts.
    /// 
    /// This is a cross-aggregate operation:
    /// - Gets Subscription aggregate from repository
    /// - Gets Plan aggregate from repository
    /// - Validates subscription and plan state
    /// - Calculates invoice amounts (base, tax, overage)
    /// - Creates Invoice aggregate
    /// - Saves invoice to repository
    /// </summary>
    public class CreateInvoiceService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly Plans.IPlanRepository _planRepository;
        private readonly IInvoiceNumberGenerator _invoiceNumberGenerator;

        public CreateInvoiceService(
            ISubscriptionRepository subscriptionRepository,
            IInvoiceRepository invoiceRepository,
            Plans.IPlanRepository planRepository,
            IInvoiceNumberGenerator invoiceNumberGenerator)
        {
            _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
            _invoiceNumberGenerator = invoiceNumberGenerator ?? throw new ArgumentNullException(nameof(invoiceNumberGenerator));
        }

        /// <summary>
        /// Creates an invoice for a subscription for a given billing period.
        /// 
        /// Workflow:
        /// 1. Validates subscription exists and is active
        /// 2. Gets subscription's plan
        /// 3. Calculates invoice amounts (base from plan, tax, overages)
        /// 4. Generates unique invoice number
        /// 5. Creates invoice aggregate
        /// 6. Saves invoice
        /// 
        /// </summary>
        /// <param name="subscriptionId">The subscription to invoice</param>
        /// <param name="billingPeriodStart">Start of billing period</param>
        /// <param name="billingPeriodEnd">End of billing period</param>
        /// <param name="taxRate">Tax rate to apply (0-1)</param>
        /// <param name="overageCharges">Any additional overage charges</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result containing created invoice or error</returns>
        public async Task<Result<Invoice>> CreateAsync(
            Guid subscriptionId,
            DateTime billingPeriodStart,
            DateTime billingPeriodEnd,
            decimal taxRate = 0,
            decimal overageCharges = 0,
            CancellationToken cancellationToken = default)
        {
            // Validation
            if (subscriptionId == Guid.Empty)
                return Result.Failure<Invoice>(
                    Error.Custom("CreateInvoice.InvalidSubscription", 
                        "Subscription ID cannot be empty."));

            if (billingPeriodStart >= billingPeriodEnd)
                return Result.Failure<Invoice>(
                    Error.Custom("CreateInvoice.InvalidBillingPeriod", 
                        "Billing period start must be before end."));

            if (taxRate < 0 || taxRate > 1)
                return Result.Failure<Invoice>(
                    Error.Custom("CreateInvoice.InvalidTaxRate", 
                        "Tax rate must be between 0 and 1."));

            if (overageCharges < 0)
                return Result.Failure<Invoice>(
                    Error.Custom("CreateInvoice.InvalidOverageCharges", 
                        "Overage charges cannot be negative."));

            // Step 1: Get subscription
            var subscription = await _subscriptionRepository.GetByIdAsync(
                subscriptionId, cancellationToken);

            if (subscription is null)
                return Result.Failure<Invoice>(SubscriptionErrors.SubscriptionNotFound);

            // Step 2: Validate subscription is active
            if (subscription.Status != SubscriptionStatus.Active)
                return Result.Failure<Invoice>(
                    Error.Custom("CreateInvoice.SubscriptionNotActive", 
                        "Can only create invoices for active subscriptions."));

            // Step 3: Get subscription's plan
            var plan = await _planRepository.GetByIdAsync(
                subscription.PlanId, cancellationToken);

            if (plan is null)
                return Result.Failure<Invoice>(
                    Plans.PlanErrors.PlanNotFound);

            // Step 4: Get pricing for subscription's billing cycle
            var pricing = plan.GetPrice(subscription.BillingCycle);
            if (pricing is null)
                return Result.Failure<Invoice>(
                    Error.Custom("CreateInvoice.NoPricingFound", 
                        "No pricing configured for subscription's billing cycle."));

            // Step 5: Calculate amounts
            var baseAmount = pricing.Amount;
            var taxAmount = baseAmount * taxRate;
            var invoiceAmount = new InvoiceAmount(
                baseAmount: baseAmount,
                overageAmount: overageCharges,
                taxAmount: taxAmount,
                discount: 0,
                currency: "USD");

            // Step 6: Create billing period
            var billingPeriod = new BillingPeriod(billingPeriodStart, billingPeriodEnd);

            // Step 7: Create usage details from subscription
            var usageDetails = new UsageDetails(
                honeypotsUsed: subscription.Usage.HoneypotsUsed,
                storageUsedGb: subscription.Usage.StorageUsedGb,
                overageCharges: overageCharges);

            // Step 8: Create tax info
            var taxInfo = new TaxInfo(taxId: null, taxRate: taxRate);

            // Step 9: Generate invoice number
            var invoiceNumberResult = await _invoiceNumberGenerator.GenerateAsync(
                subscription.OrganizationId, cancellationToken);

            if (invoiceNumberResult.IsFailure)
                return Result.Failure<Invoice>(invoiceNumberResult.Errors);

            // Step 10: Create invoice aggregate
            var invoiceResult = Invoice.Create(
                subscriptionId: subscription.Id,
                organizationId: subscription.OrganizationId,
                invoiceNumber: invoiceNumberResult.Value,
                billingPeriod: billingPeriod,
                amount: invoiceAmount,
                usageDetails: usageDetails,
                taxInfo: taxInfo);

            if (invoiceResult.IsFailure)
                return Result.Failure<Invoice>(invoiceResult.Errors);

            var invoice = invoiceResult.Value;

            // Step 11: Save invoice
            await _invoiceRepository.AddAsync(invoice, cancellationToken);

            return Result.Success(invoice);
        }

        /// <summary>
        /// Creates invoices for multiple subscriptions (bulk invoicing).
        /// Used for monthly or periodic invoice generation.
        /// </summary>
        public async Task<Result<(int SuccessCount, int FailureCount)>> CreateBulkAsync(
            DateTime billingPeriodStart,
            DateTime billingPeriodEnd,
            decimal taxRate = 0,
            CancellationToken cancellationToken = default)
        {
            if (billingPeriodStart >= billingPeriodEnd)
                return Result.Failure<(int, int)>(
                    Error.Custom("CreateInvoice.InvalidBillingPeriod", 
                        "Billing period start must be before end."));

            // TODO: Implement bulk invoice creation
            // This would iterate through all active subscriptions and create invoices
            // Would need to be implemented in application layer with repository filtering

            return Result.Success((0, 0));
        }
    }

    /// <summary>
    /// Interface for generating unique invoice numbers.
    /// Abstraction to support different numbering schemes.
    /// </summary>
    public interface IInvoiceNumberGenerator
    {
        /// <summary>
        /// Generate a unique invoice number for an organization.
        /// </summary>
        Task<Result<InvoiceNumber>> GenerateAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default);
    }
}
