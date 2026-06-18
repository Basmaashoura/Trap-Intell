using System;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Domain.Billing.Services
{
    /// <summary>
    /// Domain service for calculating invoice amounts with all components.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (invoice calculations)
    /// ? NO repositories or infrastructure dependencies
    /// ? Works with domain value objects only
    /// ? Encapsulates domain knowledge about invoice pricing
    /// 
    /// BEST PRACTICES FOLLOWED:
    /// - Stateless (no instance state)
    /// - Pure functions (deterministic calculations)
    /// - Single Responsibility (only invoice amount calculations)
    /// - Domain-driven (uses InvoiceAmount, BillingPeriod from domain)
    /// 
    /// EVIDENCE FROM CODE ANALYSIS:
    /// In InvoiceAmount value object:
    ///   public decimal TotalAmount => BaseAmount + OverageAmount + TaxAmount - Discount;
    ///   // ?? Simple addition! No discount logic, late fees, early payment discounts!
    /// 
    /// This service provides comprehensive invoice calculation logic.
    /// </summary>
    public class InvoiceAmountCalculator
    {
        // Business rule constants
        private const decimal DefaultEarlyPaymentDiscountPercent = 2.0m; // 2% for 30+ days early
        private const decimal DefaultLateFeePercent = 5.0m; // 5% base late fee
        private const decimal DefaultDailyLateFeePercent = 0.1m; // 0.1% per day
        private const decimal MaxLateFeePercent = 25.0m; // Cap at 25% of original amount

        /// <summary>
        /// Calculate complete invoice amount with all components.
        /// 
        /// BUSINESS RULES:
        /// - Subtotal = Base + Overage
        /// - Discount applied to subtotal
        /// - Tax applied to (subtotal - discount)
        /// - Total = subtotal - discount + tax
        /// 
        /// EXAMPLE:
        /// - Base: $100, Overage: $20, Tax: 10%, Discount: 5%
        /// - Subtotal: $120
        /// - After discount: $114
        /// - Tax: $11.40
        /// - Total: $125.40
        /// </summary>
        /// <param name="baseAmount">Base subscription amount</param>
        /// <param name="overageAmount">Overage charges</param>
        /// <param name="taxRate">Tax rate (0-1)</param>
        /// <param name="discountPercent">Discount percentage (0-100)</param>
        /// <param name="currency">Currency code (default: USD)</param>
        /// <returns>Complete invoice amount breakdown</returns>
        public InvoiceAmount CalculateTotal(
            decimal baseAmount,
            decimal overageAmount,
            decimal taxRate,
            decimal discountPercent = 0,
            string currency = "USD")
        {
            // Validation
            if (baseAmount < 0)
                throw new ArgumentException("Base amount cannot be negative.", nameof(baseAmount));

            if (overageAmount < 0)
                throw new ArgumentException("Overage amount cannot be negative.", nameof(overageAmount));

            if (taxRate < 0 || taxRate > 1)
                throw new ArgumentException("Tax rate must be between 0 and 1.", nameof(taxRate));

            if (discountPercent < 0 || discountPercent > 100)
                throw new ArgumentException("Discount percent must be between 0 and 100.", nameof(discountPercent));

            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency cannot be empty.", nameof(currency));

            // Calculate components
            var subtotal = baseAmount + overageAmount;
            var discountAmount = subtotal * (discountPercent / 100);
            var afterDiscount = subtotal - discountAmount;
            var taxAmount = afterDiscount * taxRate;

            return new InvoiceAmount(
                baseAmount: baseAmount,
                overageAmount: overageAmount,
                taxAmount: taxAmount,
                discount: discountAmount,
                currency: currency);
        }

        /// <summary>
        /// Calculate early payment discount for paying before due date.
        /// 
        /// BUSINESS RULES:
        /// - 30+ days early: 2% discount
        /// - 15-29 days early: 1% discount
        /// - 7-14 days early: 0.5% discount
        /// - Less than 7 days: no discount
        /// 
        /// EXAMPLE:
        /// - Invoice: $1000, paid 35 days early
        /// - Discount: $1000 * 2% = $20
        /// </summary>
        /// <param name="amount">Invoice amount</param>
        /// <param name="daysBeforeDue">Days before due date</param>
        /// <returns>Early payment discount amount</returns>
        public decimal CalculateEarlyPaymentDiscount(
            decimal amount,
            int daysBeforeDue)
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative.", nameof(amount));

            if (daysBeforeDue <= 0)
                return 0; // Not early, no discount

            var discountPercent = daysBeforeDue switch
            {
                >= 30 => 2.0m,   // 2% for 30+ days
                >= 15 => 1.0m,   // 1% for 15-29 days
                >= 7 => 0.5m,    // 0.5% for 7-14 days
                _ => 0           // No discount for < 7 days
            };

            return amount * (discountPercent / 100);
        }

        /// <summary>
        /// Calculate late fee for overdue invoices.
        /// 
        /// BUSINESS RULES:
        /// - Base late fee: 5% of original amount
        /// - Daily late fee: 0.1% per day overdue
        /// - Maximum: 25% of original amount (cap to prevent excessive fees)
        /// 
        /// EXAMPLE:
        /// - Invoice: $1000, 45 days overdue
        /// - Base fee: $50 (5%)
        /// - Daily fee: $45 (45 * 0.1%)
        /// - Total: $95 (capped at $250 max)
        /// </summary>
        /// <param name="amount">Original invoice amount</param>
        /// <param name="daysOverdue">Days past due date</param>
        /// <returns>Late fee amount</returns>
        public decimal CalculateLateFee(
            decimal amount,
            int daysOverdue)
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative.", nameof(amount));

            if (daysOverdue <= 0)
                return 0; // Not overdue, no fee

            // Calculate base late fee
            var baseFee = amount * (DefaultLateFeePercent / 100);

            // Calculate daily late fee
            var dailyFee = amount * (DefaultDailyLateFeePercent / 100) * daysOverdue;

            // Total late fee
            var totalLateFee = baseFee + dailyFee;

            // Cap at maximum percentage
            var maxFee = amount * (MaxLateFeePercent / 100);

            return Math.Min(totalLateFee, maxFee);
        }

        /// <summary>
        /// Calculate refund amount for cancelled invoice.
        /// 
        /// BUSINESS RULES:
        /// - Refund unused portion based on days remaining
        /// - Subtract processing fee (default 5%)
        /// - Minimum refund: $0 (no negative refunds)
        /// 
        /// EXAMPLE:
        /// - Invoice: $1000 for 30-day period
        /// - Cancelled after 10 days (20 days unused)
        /// - Unused portion: $666.67
        /// - Processing fee: $33.33 (5%)
        /// - Refund: $633.34
        /// </summary>
        /// <param name="paidAmount">Amount paid</param>
        /// <param name="billingPeriod">Billing period</param>
        /// <param name="cancellationDate">Date of cancellation</param>
        /// <param name="processingFeePercent">Processing fee percentage (default 5%)</param>
        /// <returns>Refund amount</returns>
        public decimal CalculateRefundAmount(
            decimal paidAmount,
            BillingPeriod billingPeriod,
            DateTime cancellationDate,
            decimal processingFeePercent = 5.0m)
        {
            if (paidAmount < 0)
                throw new ArgumentException("Paid amount cannot be negative.", nameof(paidAmount));

            if (billingPeriod == null)
                throw new ArgumentNullException(nameof(billingPeriod));

            if (processingFeePercent < 0 || processingFeePercent > 100)
                throw new ArgumentException("Processing fee must be between 0 and 100.", nameof(processingFeePercent));

            // If cancellation is after period ends, no refund
            if (cancellationDate >= billingPeriod.EndDate)
                return 0;

            // If cancellation is before period starts, full refund minus processing fee
            if (cancellationDate <= billingPeriod.StartDate)
            {
                var processingFee = paidAmount * (processingFeePercent / 100);
                return Math.Max(0, paidAmount - processingFee);
            }

            // Calculate unused portion
            var totalDays = billingPeriod.DaysInPeriod;
            var daysUsed = (cancellationDate - billingPeriod.StartDate).Days;
            var daysUnused = totalDays - daysUsed;

            var unusedPortion = paidAmount * (daysUnused / (decimal)totalDays);

            // Subtract processing fee
            var processingFeeAmount = unusedPortion * (processingFeePercent / 100);
            var refundAmount = unusedPortion - processingFeeAmount;

            return Math.Max(0, refundAmount);
        }

        /// <summary>
        /// Calculate prorated amount for mid-period subscription change.
        /// 
        /// BUSINESS RULES:
        /// - Calculate daily rate from original amount
        /// - Multiply by days remaining
        /// - Used for upgrades/downgrades
        /// 
        /// EXAMPLE:
        /// - Original: $300 for 30 days = $10/day
        /// - Changed on day 20 (10 days remaining)
        /// - Prorated: $100
        /// </summary>
        /// <param name="originalAmount">Original billing amount</param>
        /// <param name="billingPeriod">Billing period</param>
        /// <param name="changeDate">Date of change</param>
        /// <returns>Prorated amount for remaining days</returns>
        public decimal CalculateProratedAmount(
            decimal originalAmount,
            BillingPeriod billingPeriod,
            DateTime changeDate)
        {
            if (originalAmount < 0)
                throw new ArgumentException("Original amount cannot be negative.", nameof(originalAmount));

            if (billingPeriod == null)
                throw new ArgumentNullException(nameof(billingPeriod));

            // Validate change date is within period
            if (changeDate < billingPeriod.StartDate)
                return 0; // Change is before period starts

            if (changeDate >= billingPeriod.EndDate)
                return 0; // Change is after period ends

            // Calculate daily rate
            var dailyRate = billingPeriod.GetDailyRate(originalAmount);

            // Calculate days remaining
            var daysRemaining = (billingPeriod.EndDate - changeDate).Days;

            return dailyRate * daysRemaining;
        }

        /// <summary>
        /// Calculate upgrade credit for moving to higher plan mid-period.
        /// 
        /// BUSINESS RULES:
        /// - Credit = prorated value of old plan for remaining days
        /// - This credit offsets the new plan cost
        /// 
        /// EXAMPLE:
        /// - Old plan: $100/month, 15 days remaining = $50 credit
        /// - New plan: $200/month, 15 days remaining = $100 charge
        /// - Net charge: $100 - $50 = $50
        /// </summary>
        /// <param name="oldPlanAmount">Old plan monthly amount</param>
        /// <param name="billingPeriod">Current billing period</param>
        /// <param name="upgradeDate">Date of upgrade</param>
        /// <returns>Credit amount from old plan</returns>
        public decimal CalculateUpgradeCredit(
            decimal oldPlanAmount,
            BillingPeriod billingPeriod,
            DateTime upgradeDate)
        {
            return CalculateProratedAmount(oldPlanAmount, billingPeriod, upgradeDate);
        }

        /// <summary>
        /// Calculate net amount owed after applying credits.
        /// 
        /// EXAMPLE:
        /// - New charge: $100
        /// - Credits: $30
        /// - Net owed: $70
        /// </summary>
        /// <param name="grossAmount">Gross amount before credits</param>
        /// <param name="credits">Total credits to apply</param>
        /// <returns>Net amount owed (minimum 0)</returns>
        public decimal CalculateNetAmountAfterCredits(
            decimal grossAmount,
            decimal credits)
        {
            if (grossAmount < 0)
                throw new ArgumentException("Gross amount cannot be negative.", nameof(grossAmount));

            if (credits < 0)
                throw new ArgumentException("Credits cannot be negative.", nameof(credits));

            return Math.Max(0, grossAmount - credits);
        }

        /// <summary>
        /// Calculate payment fee based on payment method.
        /// 
        /// BUSINESS RULES:
        /// - Credit Card: 2.9% + $0.30
        /// - Bank Transfer: $5 flat
        /// - Digital Wallet: 3.5%
        /// </summary>
        /// <param name="amount">Payment amount</param>
        /// <param name="paymentMethod">Payment method type</param>
        /// <returns>Payment processing fee</returns>
        public decimal CalculatePaymentFee(
            decimal amount,
            PaymentMethodType paymentMethod)
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative.", nameof(amount));

            return paymentMethod switch
            {
                PaymentMethodType.CreditCard => (amount * 0.029m) + 0.30m, // 2.9% + $0.30
                PaymentMethodType.BankTransfer => 5.0m, // $5 flat fee
                PaymentMethodType.DigitalWallet => amount * 0.035m, // 3.5%
                _ => 0
            };
        }

        /// <summary>
        /// Calculate compound discount when multiple discounts apply.
        /// 
        /// BUSINESS RULES:
        /// - Discounts are applied sequentially (compound), not additively
        /// - Example: 10% + 5% = 14.5% total (not 15%)
        /// </summary>
        /// <param name="baseAmount">Base amount</param>
        /// <param name="discountPercents">List of discount percentages</param>
        /// <returns>Total discount amount</returns>
        public decimal CalculateCompoundDiscount(
            decimal baseAmount,
            params decimal[] discountPercents)
        {
            if (baseAmount < 0)
                throw new ArgumentException("Base amount cannot be negative.", nameof(baseAmount));

            var remaining = baseAmount;

            foreach (var percent in discountPercents)
            {
                if (percent < 0 || percent > 100)
                    throw new ArgumentException($"Discount percent must be between 0 and 100. Got: {percent}");

                remaining -= remaining * (percent / 100);
            }

            return baseAmount - remaining;
        }
    }

    /// <summary>
    /// Payment method types for fee calculation.
    /// </summary>
    public enum PaymentMethodType
    {
        CreditCard,
        BankTransfer,
        DigitalWallet
    }
}
