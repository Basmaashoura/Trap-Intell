using System;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Billing
{
    /// <summary>
    /// Domain service that coordinates payment processing.
    /// Handles the complex workflow of processing payments for invoices.
    /// 
    /// This is a cross-aggregate operation:
    /// - Gets Invoice aggregate from repository
    /// - Gets PaymentMethod aggregate from repository
    /// - Validates both are in correct state
    /// - Coordinates with payment processor (external)
    /// - Updates both invoice and payment method
    /// - Creates payment record
    /// - Raises domain events
    /// </summary>
    public class ProcessPaymentService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        private readonly IPaymentProcessor _paymentProcessor;

        public ProcessPaymentService(
            IInvoiceRepository invoiceRepository,
            IPaymentMethodRepository paymentMethodRepository,
            IPaymentProcessor paymentProcessor)
        {
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _paymentMethodRepository = paymentMethodRepository ?? throw new ArgumentNullException(nameof(paymentMethodRepository));
            _paymentProcessor = paymentProcessor ?? throw new ArgumentNullException(nameof(paymentProcessor));
        }

        /// <summary>
        /// Process payment for an invoice using a payment method.
        /// 
        /// Workflow:
        /// 1. Get invoice from repository
        /// 2. Validate invoice is in issuable/overdue state
        /// 3. Get payment method from repository
        /// 4. Validate payment method is usable
        /// 5. Validate payment method has permission to charge
        /// 6. Call payment processor to charge
        /// 7. If successful: mark invoice as paid, link payment ID
        /// 8. If failed: capture error, allow retry
        /// 9. Save changes
        /// 10. Raise domain events
        /// </summary>
        /// <param name="invoiceId">Invoice to pay</param>
        /// <param name="paymentMethodId">Payment method to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result with payment ID if successful</returns>
        public async Task<Result<Guid>> ProcessAsync(
            Guid invoiceId,
            Guid paymentMethodId,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default)
        {
            // Validation
            if (invoiceId == Guid.Empty)
                return Result.Failure<Guid>(
                    Error.Custom("ProcessPayment.InvalidInvoice", 
                        "Invoice ID cannot be empty."));

            if (paymentMethodId == Guid.Empty)
                return Result.Failure<Guid>(
                    Error.Custom("ProcessPayment.InvalidPaymentMethod", 
                        "Payment method ID cannot be empty."));

            // Step 1: Get invoice
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);

            if (invoice is null)
                return Result.Failure<Guid>(BillingErrors.InvoiceNotFound);

            // Step 2: Validate invoice can be paid
            var paymentRule = new InvoicePaymentRule(invoice);
            if (!paymentRule.IsSatisfied())
                return Result.Failure<Guid>(BillingErrors.InvoiceCannotMarkPaid);

            // Step 3: Get payment method
            var paymentMethod = await _paymentMethodRepository.GetByIdAsync(
                paymentMethodId, cancellationToken);

            if (paymentMethod is null)
                return Result.Failure<Guid>(BillingErrors.PaymentMethodNotFound);

            if (paymentMethod.OrganizationId != invoice.OrganizationId)
                return Result.Failure<Guid>(BillingErrors.PaymentMethodNotFound);

            // Step 4: Validate payment method is usable
            var usabilityRule = new PaymentMethodUsabilityRule(paymentMethod);
            if (!usabilityRule.IsSatisfied())
                return Result.Failure<Guid>(BillingErrors.PaymentMethodNotUsable);

            // Step 5: Validate payment method can be charged (async check with processor)
            var chargeableRule = new PaymentMethodChargeableRule(paymentMethod);
            if (!await chargeableRule.IsSatisfiedAsync(cancellationToken))
                return Result.Failure<Guid>(BillingErrors.PaymentMethodNotUsable);

            // Step 6: Call payment processor
            var normalizedIdempotencyKey = BillingIdempotency.NormalizeKey(idempotencyKey);

            var processorResult = normalizedIdempotencyKey is null
                ? await _paymentProcessor.ChargeAsync(
                    paymentMethod: paymentMethod,
                    amount: invoice.Amount.TotalAmount,
                    currency: invoice.Amount.Currency,
                    invoiceNumber: invoice.InvoiceNumber.Value,
                    description: $"Invoice {invoice.InvoiceNumber.Value} for organization {invoice.OrganizationId}",
                    cancellationToken: cancellationToken)
                : await _paymentProcessor.ChargeAsync(
                    paymentMethod: paymentMethod,
                    amount: invoice.Amount.TotalAmount,
                    currency: invoice.Amount.Currency,
                    invoiceNumber: invoice.InvoiceNumber.Value,
                    description: $"Invoice {invoice.InvoiceNumber.Value} for organization {invoice.OrganizationId}",
                    idempotencyKey: normalizedIdempotencyKey,
                    cancellationToken: cancellationToken);

            if (processorResult.IsFailure)
                return Result.Failure<Guid>(processorResult.Errors);

            var paymentId = processorResult.Value;

            // Step 7: Mark invoice as paid
            var markPaidResult = invoice.MarkAsPaid(paymentId);
            if (markPaidResult.IsFailure)
                return Result.Failure<Guid>(markPaidResult.Errors);

            // Step 8: Save invoice
            await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

            return Result.Success(paymentId);
        }

        /// <summary>
        /// Process payment for an invoice using the default payment method.
        /// </summary>
        public async Task<Result<Guid>> ProcessWithDefaultAsync(
            Guid invoiceId,
            Guid organizationId,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default)
        {
            if (invoiceId == Guid.Empty)
                return Result.Failure<Guid>(
                    Error.Custom("ProcessPayment.InvalidInvoice", 
                        "Invoice ID cannot be empty."));

            if (organizationId == Guid.Empty)
                return Result.Failure<Guid>(
                    Error.Custom("ProcessPayment.InvalidOrganization", 
                        "Organization ID cannot be empty."));

            // Get organization's default payment method
            var defaultPaymentMethod = await _paymentMethodRepository.GetDefaultByOrganizationIdAsync(
                organizationId, cancellationToken);

            if (defaultPaymentMethod is null)
                return Result.Failure<Guid>(BillingErrors.PaymentMethodNoDefault);

            // Process with default payment method
            return await ProcessAsync(
                invoiceId,
                defaultPaymentMethod.Id,
                idempotencyKey,
                cancellationToken);
        }

        /// <summary>
        /// Retry payment for a failed invoice.
        /// </summary>
        public async Task<Result<Guid>> RetryAsync(
            Guid invoiceId,
            Guid paymentMethodId,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default)
        {
            // Same as normal process, but with additional retry logic
            // Could implement retry count limiting, exponential backoff, etc.
            
            return await ProcessAsync(invoiceId, paymentMethodId, idempotencyKey, cancellationToken);
        }

        /// <summary>
        /// Process batch payments for multiple invoices.
        /// </summary>
        public async Task<Result<(int SuccessCount, int FailureCount)>> ProcessBatchAsync(
            Guid[] invoiceIds,
            Guid paymentMethodId,
            CancellationToken cancellationToken = default)
        {
            if (invoiceIds is null || invoiceIds.Length == 0)
                return Result.Failure<(int, int)>(
                    Error.Custom("ProcessPayment.EmptyBatch", 
                        "Invoice list cannot be empty."));

            if (paymentMethodId == Guid.Empty)
                return Result.Failure<(int, int)>(
                    Error.Custom("ProcessPayment.InvalidPaymentMethod", 
                        "Payment method ID cannot be empty."));

            int successCount = 0;
            int failureCount = 0;

            foreach (var invoiceId in invoiceIds)
            {
                var result = await ProcessAsync(
                    invoiceId,
                    paymentMethodId,
                    cancellationToken: cancellationToken);
                
                if (result.IsSuccess)
                    successCount++;
                else
                    failureCount++;
            }

            return Result.Success((successCount, failureCount));
        }
    }

    /// <summary>
    /// Interface for external payment processor (Stripe, PayPal, etc).
    /// Abstraction for payment processing operations.
    /// </summary>
    public interface IPaymentProcessor
    {
        /// <summary>
        /// Charge a payment method for the specified amount.
        /// </summary>
        Task<Result<Guid>> ChargeAsync(
            PaymentMethod paymentMethod,
            decimal amount,
            string currency,
            string invoiceNumber,
            string description,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Charge a payment method using an explicit idempotency key.
        /// </summary>
        Task<Result<Guid>> ChargeAsync(
            PaymentMethod paymentMethod,
            decimal amount,
            string currency,
            string invoiceNumber,
            string description,
            string idempotencyKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Refund a previous payment.
        /// </summary>
        Task<Result<Guid>> RefundAsync(
            Guid paymentId,
            decimal amount,
            string reason,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Refund a previous payment using an explicit idempotency key.
        /// </summary>
        Task<Result<Guid>> RefundAsync(
            Guid paymentId,
            decimal amount,
            string reason,
            string idempotencyKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verify payment method is valid with processor.
        /// </summary>
        Task<Result<bool>> VerifyAsync(
            PaymentMethod paymentMethod,
            CancellationToken cancellationToken = default);
    }
}
