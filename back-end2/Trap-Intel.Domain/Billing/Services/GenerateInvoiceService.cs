using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Domain.Billing
{
    /// <summary>
    /// Domain service that generates invoices periodically.
    /// Handles the complex workflow of bulk invoice generation.
    /// 
    /// This is a cross-aggregate operation:
    /// - Gets all active subscriptions for a period
    /// - For each subscription: creates corresponding invoice
    /// - Calculates proper amounts (base, tax, usage)
    /// - Generates unique invoice numbers
    /// - Saves all invoices
    /// - Handles partial failures gracefully
    /// </summary>
    public class GenerateInvoiceService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly Plans.IPlanRepository _planRepository;
        private readonly IInvoiceNumberGenerator _invoiceNumberGenerator;
        private readonly ITaxRateProvider _taxRateProvider;

        public GenerateInvoiceService(
            ISubscriptionRepository subscriptionRepository,
            IInvoiceRepository invoiceRepository,
            Plans.IPlanRepository planRepository,
            IInvoiceNumberGenerator invoiceNumberGenerator,
            ITaxRateProvider taxRateProvider)
        {
            _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
            _invoiceNumberGenerator = invoiceNumberGenerator ?? throw new ArgumentNullException(nameof(invoiceNumberGenerator));
            _taxRateProvider = taxRateProvider ?? throw new ArgumentNullException(nameof(taxRateProvider));
        }

        /// <summary>
        /// Generate invoices for all active subscriptions with specified billing cycle.
        /// 
        /// Workflow:
        /// 1. Get all active subscriptions
        /// 2. Filter by billing cycle
        /// 3. For each subscription:
        ///    a. Get plan
        ///    b. Get tax rate for organization
        ///    c. Calculate usage/overages
        ///    d. Create invoice
        ///    e. Save invoice
        /// 4. Return results (success/failure counts)
        /// </summary>
        /// <param name="billingPeriodStart">Billing period start</param>
        /// <param name="billingPeriodEnd">Billing period end</param>
        /// <param name="billingCycle">Specific cycle to generate for (or null for all)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result with counts of generated/failed invoices</returns>
        public async Task<Result<InvoiceGenerationResult>> GenerateAsync(
            DateTime billingPeriodStart,
            DateTime billingPeriodEnd,
            BillingCycle? billingCycle = null,
            CancellationToken cancellationToken = default)
        {
            // Validation
            if (billingPeriodStart >= billingPeriodEnd)
                return Result.Failure<InvoiceGenerationResult>(
                    Error.Custom("GenerateInvoice.InvalidPeriod", 
                        "Billing period start must be before end."));

            var result = new InvoiceGenerationResult();

            try
            {
                // Step 1: Get all active subscriptions
                // NOTE: This requires repository support for filtering by status
                // For now, this is a placeholder - actual implementation depends on repo capability
                var subscriptions = new List<Subscription>();
                // var subscriptions = await _subscriptionRepository.GetActiveAsync(cancellationToken);

                // Step 2: Process each subscription
                foreach (var subscription in subscriptions)
                {
                    try
                    {
                        // Skip if billing cycle doesn't match (if specified)
                        if (billingCycle.HasValue && subscription.BillingCycle != billingCycle)
                            continue;

                        // Get subscription's plan
                        var plan = await _planRepository.GetByIdAsync(
                            subscription.PlanId, cancellationToken);

                        if (plan is null)
                        {
                            result.FailedCount++;
                            result.Errors.Add(
                                $"Subscription {subscription.Id}: Plan not found");
                            continue;
                        }

                        // Get pricing
                        var pricing = plan.GetPrice(subscription.BillingCycle);
                        if (pricing is null)
                        {
                            result.FailedCount++;
                            result.Errors.Add(
                                $"Subscription {subscription.Id}: No pricing configured");
                            continue;
                        }

                        // Get tax rate for organization
                        var taxRateResult = await _taxRateProvider.GetTaxRateAsync(
                            subscription.OrganizationId, cancellationToken);

                        var taxRate = taxRateResult.IsSuccess ? taxRateResult.Value : 0;

                        // Create billing period
                        var billingPeriod = new BillingPeriod(billingPeriodStart, billingPeriodEnd);

                        // Calculate amounts
                        var baseAmount = pricing.Amount;
                        var taxAmount = baseAmount * taxRate;
                        var invoiceAmount = new InvoiceAmount(
                            baseAmount: baseAmount,
                            overageAmount: subscription.Usage.OverageCharges,
                            taxAmount: taxAmount,
                            discount: 0,
                            currency: "USD");

                        // Create usage details
                        var usageDetails = new UsageDetails(
                            honeypotsUsed: subscription.Usage.HoneypotsUsed,
                            storageUsedGb: subscription.Usage.StorageUsedGb,
                            overageCharges: subscription.Usage.OverageCharges);

                        // Create tax info
                        var taxInfo = new TaxInfo(taxId: null, taxRate: taxRate);

                        // Generate invoice number
                        var invoiceNumberResult = await _invoiceNumberGenerator.GenerateAsync(
                            subscription.OrganizationId, cancellationToken);

                        if (invoiceNumberResult.IsFailure)
                        {
                            result.FailedCount++;
                            result.Errors.Add(
                                $"Subscription {subscription.Id}: Failed to generate invoice number");
                            continue;
                        }

                        // Create invoice
                        var invoiceResult = Invoice.Create(
                            subscriptionId: subscription.Id,
                            organizationId: subscription.OrganizationId,
                            invoiceNumber: invoiceNumberResult.Value,
                            billingPeriod: billingPeriod,
                            amount: invoiceAmount,
                            usageDetails: usageDetails,
                            taxInfo: taxInfo);

                        if (invoiceResult.IsFailure)
                        {
                            result.FailedCount++;
                            result.Errors.Add(
                                $"Subscription {subscription.Id}: Failed to create invoice");
                            continue;
                        }

                        // Save invoice
                        var invoice = invoiceResult.Value;
                        await _invoiceRepository.AddAsync(invoice, cancellationToken);

                        result.SuccessCount++;
                        result.GeneratedInvoices.Add(invoice.Id);
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Subscription {subscription.Id}: {ex.Message}");
                    }
                }

                return Result.Success(result);
            }
            catch (Exception ex)
            {
                return Result.Failure<InvoiceGenerationResult>(
                    Error.Custom("GenerateInvoice.ProcessingFailed", ex.Message));
            }
        }

        /// <summary>
        /// Generate invoices for a specific organization.
        /// </summary>
        public async Task<Result<InvoiceGenerationResult>> GenerateForOrganizationAsync(
            Guid organizationId,
            DateTime billingPeriodStart,
            DateTime billingPeriodEnd,
            CancellationToken cancellationToken = default)
        {
            if (organizationId == Guid.Empty)
                return Result.Failure<InvoiceGenerationResult>(
                    Error.Custom("GenerateInvoice.InvalidOrganization", 
                        "Organization ID cannot be empty."));

            // TODO: Implement organization-specific invoice generation
            // This would filter subscriptions by organization before generating

            return await GenerateAsync(billingPeriodStart, billingPeriodEnd, null, cancellationToken);
        }

        /// <summary>
        /// Generate invoices for a specific billing cycle (e.g., all monthly subscriptions).
        /// </summary>
        public async Task<Result<InvoiceGenerationResult>> GenerateForBillingCycleAsync(
            BillingCycle billingCycle,
            DateTime billingPeriodStart,
            DateTime billingPeriodEnd,
            CancellationToken cancellationToken = default)
        {
            return await GenerateAsync(
                billingPeriodStart,
                billingPeriodEnd,
                billingCycle,
                cancellationToken);
        }
    }

    /// <summary>
    /// Result object containing invoice generation statistics.
    /// </summary>
    public class InvoiceGenerationResult
    {
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<Guid> GeneratedInvoices { get; } = new();
        public List<string> Errors { get; } = new();

        public int TotalCount => SuccessCount + FailedCount;
        public double SuccessRate => TotalCount > 0 
            ? (double)SuccessCount / TotalCount * 100 
            : 0;
    }

    /// <summary>
    /// Interface for providing tax rates by organization.
    /// Abstraction for tax calculation logic.
    /// </summary>
    public interface ITaxRateProvider
    {
        /// <summary>
        /// Get tax rate for an organization.
        /// </summary>
        Task<Result<decimal>> GetTaxRateAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get tax rate for a specific jurisdiction.
        /// </summary>
        Task<Result<decimal>> GetTaxRateByJurisdictionAsync(
            string jurisdiction,
            CancellationToken cancellationToken = default);
    }
}
