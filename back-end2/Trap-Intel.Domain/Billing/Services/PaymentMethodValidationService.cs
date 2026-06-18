using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Domain.Billing.Services
{
    /// <summary>
    /// Domain service for validating payment methods.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (payment method validation)
    /// ? NO repositories or infrastructure dependencies
    /// ? Works with domain objects only
    /// ? Encapsulates domain knowledge about payment validation rules
    /// 
    /// BEST PRACTICES FOLLOWED:
    /// - Stateless (no instance state)
    /// - Pure functions (deterministic validation)
    /// - Single Responsibility (only payment validation)
    /// - Domain-driven (uses PaymentMethod domain object)
    /// 
    /// EVIDENCE FROM CODE ANALYSIS:
    /// In PaymentMethodDetails:
    ///   public bool IsExpired => ExpiresAt.HasValue && ExpiresAt <= DateTime.UtcNow;
    ///   // ?? Simple check. Need comprehensive validation!
    /// 
    /// This service provides comprehensive payment method validation.
    /// </summary>
    public class PaymentMethodValidationService
    {
        // Validation constants
        private const int ExpirationWarningDays = 30; // Warn if expires within 30 days

        /// <summary>
        /// Validate if payment method is valid for use.
        /// 
        /// BUSINESS RULES:
        /// - Payment method must not be expired
        /// - Payment method must be active
        /// 
        /// EXAMPLE:
        /// - Card expiry: 12/2025
        /// - Current: 01/2024
        /// - Result: ? Valid
        /// </summary>
        /// <param name="paymentMethod">Payment method to validate</param>
        /// <returns>True if payment method is valid</returns>
        public bool IsValid(PaymentMethod paymentMethod)
        {
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));

            // Use IsUsable property from entity
            return paymentMethod.IsUsable;
        }

        /// <summary>
        /// Check if payment method is expired.
        /// 
        /// BUSINESS RULE:
        /// - Expired if expiration date has passed
        /// - Payment methods without expiration never expire
        /// </summary>
        /// <param name="paymentMethod">Payment method to check</param>
        /// <returns>True if expired</returns>
        public bool IsExpired(PaymentMethod paymentMethod)
        {
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));

            return paymentMethod.Details.IsExpired;
        }

        /// <summary>
        /// Check if payment method is verified.
        /// 
        /// BUSINESS RULE:
        /// - Payment method is verified if it has a token
        /// - Indicates successful verification with payment processor
        /// </summary>
        /// <param name="paymentMethod">Payment method to check</param>
        /// <returns>True if verified</returns>
        public bool IsVerified(PaymentMethod paymentMethod)
        {
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));

            // Verified if has token and is active
            return !string.IsNullOrEmpty(paymentMethod.Details.Token) &&
                   paymentMethod.Status == Billing.PaymentMethodStatus.Active;
        }

        /// <summary>
        /// Validate payment method for charging.
        /// Comprehensive validation before processing a charge.
        /// 
        /// BUSINESS RULES:
        /// - Payment method must be usable (active and not expired)
        /// - Amount must be positive
        /// - Amount must be within limits
        /// </summary>
        /// <param name="paymentMethod">Payment method to validate</param>
        /// <param name="amount">Amount to charge</param>
        /// <returns>List of validation errors (empty if valid)</returns>
        public List<ValidationError> ValidateForCharge(
            PaymentMethod paymentMethod,
            decimal amount)
        {
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));

            var errors = new List<ValidationError>();

            // Rule 1: Amount must be positive
            if (amount <= 0)
            {
                errors.Add(new ValidationError(
                    "Amount.Invalid",
                    $"Charge amount must be greater than zero. Got: {amount:C}"));
            }

            // Rule 2: Payment method must be usable (active and not expired)
            if (!paymentMethod.IsUsable)
            {
                errors.Add(new ValidationError(
                    "PaymentMethod.NotUsable",
                    "Payment method is not usable. It may be inactive or expired."));
            }

            // Rule 3: Check if expired
            if (IsExpired(paymentMethod))
            {
                errors.Add(new ValidationError(
                    "PaymentMethod.Expired",
                    $"Payment method expired on {paymentMethod.Details.ExpiresAt:MM/yyyy}. " +
                    $"Please update your payment information."));
            }

            // Rule 4: Check minimum amount (if applicable)
            const decimal minAmount = 0.50m; // $0.50 minimum
            if (amount < minAmount)
            {
                errors.Add(new ValidationError(
                    "Amount.BelowMinimum",
                    $"Charge amount must be at least {minAmount:C}. Got: {amount:C}"));
            }

            // Rule 5: Check maximum amount (if applicable)
            const decimal maxAmount = 999999.99m; // $999,999.99 maximum
            if (amount > maxAmount)
            {
                errors.Add(new ValidationError(
                    "Amount.ExceedsMaximum",
                    $"Charge amount cannot exceed {maxAmount:C}. Got: {amount:C}"));
            }

            return errors;
        }

        /// <summary>
        /// Check if payment method is expiring soon.
        /// Used for proactive notifications.
        /// 
        /// BUSINESS RULE:
        /// - Warn if expires within 30 days
        /// - Used to notify users to update payment info
        /// </summary>
        /// <param name="paymentMethod">Payment method to check</param>
        /// <returns>True if expiring within 30 days</returns>
        public bool IsExpiringSoon(PaymentMethod paymentMethod)
        {
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));

            if (!paymentMethod.Details.ExpiresAt.HasValue)
                return false;

            var daysUntilExpiration = (paymentMethod.Details.ExpiresAt.Value - DateTime.UtcNow).Days;
            return daysUntilExpiration > 0 && daysUntilExpiration <= ExpirationWarningDays;
        }

        /// <summary>
        /// Get days until expiration.
        /// Returns negative value if already expired.
        /// </summary>
        /// <param name="paymentMethod">Payment method to check</param>
        /// <returns>Days until expiration (negative if expired)</returns>
        public int GetDaysUntilExpiration(PaymentMethod paymentMethod)
        {
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));

            if (!paymentMethod.Details.ExpiresAt.HasValue)
                return int.MaxValue; // Never expires

            return (paymentMethod.Details.ExpiresAt.Value - DateTime.UtcNow).Days;
        }

        /// <summary>
        /// Get comprehensive validation status.
        /// Includes all checks with detailed messages.
        /// </summary>
        /// <param name="paymentMethod">Payment method to validate</param>
        /// <returns>Comprehensive validation status</returns>
        public PaymentMethodValidationStatus GetValidationStatus(
            PaymentMethod paymentMethod)
        {
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));

            var issues = new List<string>();
            var warnings = new List<string>();

            // Check expiration
            if (IsExpired(paymentMethod))
            {
                issues.Add($"Expired on {paymentMethod.Details.ExpiresAt:MM/yyyy}");
            }
            else if (IsExpiringSoon(paymentMethod))
            {
                var daysRemaining = GetDaysUntilExpiration(paymentMethod);
                warnings.Add($"Expires in {daysRemaining} days on {paymentMethod.Details.ExpiresAt:MM/yyyy}");
            }

            // Check active status
            if (paymentMethod.Status != Billing.PaymentMethodStatus.Active)
            {
                issues.Add($"Status: {paymentMethod.Status}");
            }

            var isValid = issues.Count == 0;
            var status = DetermineStatus(isValid, issues.Count, warnings.Count);

            return new PaymentMethodValidationStatus(
                IsValid: isValid,
                Status: status,
                Issues: issues,
                Warnings: warnings);
        }

        /// <summary>
        /// Validate payment method details format.
        /// Check if card number, CVV, etc. are in valid format.
        /// 
        /// NOTE: This does NOT validate with payment processor.
        /// This is just format validation.
        /// </summary>
        /// <param name="cardNumber">Card number</param>
        /// <param name="cvv">CVV code</param>
        /// <param name="expiryMonth">Expiry month (1-12)</param>
        /// <param name="expiryYear">Expiry year (full year, e.g., 2025)</param>
        /// <returns>Validation result</returns>
        public PaymentDetailsValidationResult ValidatePaymentDetails(
            string cardNumber,
            string cvv,
            int expiryMonth,
            int expiryYear)
        {
            var errors = new List<string>();

            // Validate card number (Luhn algorithm)
            if (!IsValidCardNumber(cardNumber))
            {
                errors.Add("Invalid card number format");
            }

            // Validate CVV
            if (!IsValidCVV(cvv))
            {
                errors.Add("Invalid CVV format (must be 3-4 digits)");
            }

            // Validate expiry date
            if (!IsValidExpiryDate(expiryMonth, expiryYear))
            {
                errors.Add("Invalid or past expiry date");
            }

            return new PaymentDetailsValidationResult(
                IsValid: errors.Count == 0,
                Errors: errors);
        }

        #region Private Helper Methods

        /// <summary>
        /// Determine overall status from issues and warnings.
        /// </summary>
        private PaymentMethodStatus DetermineStatus(
            bool isValid,
            int issueCount,
            int warningCount)
        {
            if (!isValid)
                return PaymentMethodStatus.Invalid;

            if (warningCount > 0)
                return PaymentMethodStatus.Warning;

            return PaymentMethodStatus.Valid;
        }

        /// <summary>
        /// Validate card number using Luhn algorithm.
        /// </summary>
        private bool IsValidCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                return false;

            // Remove spaces and dashes
            cardNumber = Regex.Replace(cardNumber, @"[\s-]", "");

            // Must be digits only
            if (!Regex.IsMatch(cardNumber, @"^\d+$"))
                return false;

            // Must be 13-19 digits (standard card lengths)
            if (cardNumber.Length < 13 || cardNumber.Length > 19)
                return false;

            // Luhn algorithm
            int sum = 0;
            bool alternate = false;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int digit = cardNumber[i] - '0';

                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                alternate = !alternate;
            }

            return (sum % 10) == 0;
        }

        /// <summary>
        /// Validate CVV format.
        /// </summary>
        private bool IsValidCVV(string cvv)
        {
            if (string.IsNullOrWhiteSpace(cvv))
                return false;

            // Must be 3 or 4 digits
            return Regex.IsMatch(cvv, @"^\d{3,4}$");
        }

        /// <summary>
        /// Validate expiry date.
        /// </summary>
        private bool IsValidExpiryDate(int month, int year)
        {
            // Month must be 1-12
            if (month < 1 || month > 12)
                return false;

            // Year must be current year or future
            var currentYear = DateTime.UtcNow.Year;
            if (year < currentYear)
                return false;

            // If current year, month must be current month or future
            if (year == currentYear)
            {
                var currentMonth = DateTime.UtcNow.Month;
                if (month < currentMonth)
                    return false;
            }

            return true;
        }

        #endregion
    }

    #region Supporting Value Objects

    /// <summary>
    /// Validation error detail.
    /// </summary>
    public record ValidationError(string Code, string Message);

    /// <summary>
    /// Payment method validation status.
    /// </summary>
    public record PaymentMethodValidationStatus(
        bool IsValid,
        PaymentMethodStatus Status,
        List<string> Issues,
        List<string> Warnings)
    {
        public string GetSummary()
        {
            if (IsValid && Warnings.Count == 0)
                return "Payment method is valid";

            if (IsValid)
                return $"Payment method is valid with {Warnings.Count} warning(s)";

            return $"Payment method is invalid ({Issues.Count} issue(s))";
        }
    }

    /// <summary>
    /// Payment method status enum.
    /// </summary>
    public enum PaymentMethodStatus
    {
        Valid,      // All checks pass
        Warning,    // Valid but has warnings (e.g., expiring soon)
        Invalid     // Has issues, cannot be used
    }

    /// <summary>
    /// Payment details validation result.
    /// </summary>
    public record PaymentDetailsValidationResult(
        bool IsValid,
        List<string> Errors);

    #endregion
}
