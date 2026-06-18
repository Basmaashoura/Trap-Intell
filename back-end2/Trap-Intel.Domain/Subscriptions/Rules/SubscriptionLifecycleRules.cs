using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Subscriptions
{
    /// <summary>
    /// Additional business rules for subscription lifecycle operations.
    /// Complements existing SubscriptionBusinessRules.
    /// </summary>

    /// <summary>
    /// Rule: Usage must not exceed hard limits if enforced.
    /// </summary>
    public class SubscriptionUsageEnforcementRule : IBusinessRule
    {
        private readonly Subscription _subscription;
        private readonly int _honeypotsRequested;
        private readonly decimal _storageRequested;
        private readonly SubscriptionQuota _quota;
        public Error Error => SubscriptionErrors.SubscriptionQuotaExceeded;

        public SubscriptionUsageEnforcementRule(
            Subscription subscription,
            int honeypotsRequested,
            decimal storageRequested,
            SubscriptionQuota quota)
        {
            _subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
            _honeypotsRequested = honeypotsRequested;
            _storageRequested = storageRequested;
            _quota = quota ?? throw new ArgumentNullException(nameof(quota));
        }

        public bool IsSatisfied()
        {
            // If hard limit enforced, must not exceed
            if (_quota.HardLimitEnforced)
            {
                if (_quota.IsHoneypotLimitExceeded(_honeypotsRequested))
                    return false;

                if (_quota.IsStorageLimitExceeded(_storageRequested))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Rule: Usage warning threshold check (80%).
    /// </summary>
    public class SubscriptionUsageWarningRule : IBusinessRule
    {
        private readonly decimal _honeypotUsagePercentage;
        private readonly decimal _storageUsagePercentage;
        public Error Error => SubscriptionErrors.SubscriptionQuotaExceeded;

        public SubscriptionUsageWarningRule(
            decimal honeypotUsagePercentage,
            decimal storageUsagePercentage)
        {
            _honeypotUsagePercentage = honeypotUsagePercentage;
            _storageUsagePercentage = storageUsagePercentage;
        }

        public bool IsSatisfied()
        {
            // Warning threshold is 80%
            return _honeypotUsagePercentage < 80 && _storageUsagePercentage < 80;
        }

        public bool ShouldWarn => !IsSatisfied();
    }

    /// <summary>
    /// Rule: Subscription period must be valid for renewal.
    /// </summary>
    public class SubscriptionPeriodValidityRule : IBusinessRule
    {
        private readonly SubscriptionPeriod _currentPeriod;
        private readonly SubscriptionPeriod _newPeriod;
        public Error Error { get; }

        public SubscriptionPeriodValidityRule(
            SubscriptionPeriod currentPeriod,
            SubscriptionPeriod newPeriod)
        {
            _currentPeriod = currentPeriod ?? throw new ArgumentNullException(nameof(currentPeriod));
            _newPeriod = newPeriod ?? throw new ArgumentNullException(nameof(newPeriod));
            Error = Error.Custom("Subscription.InvalidRenewalPeriod", 
                "Renewal period must start when current period ends.");
        }

        public bool IsSatisfied()
        {
            // New period must start when current period ends
            return _newPeriod.StartDate == _currentPeriod.EndDate;
        }
    }

    /// <summary>
    /// Rule: Cancelled subscription cannot have active invoices.
    /// </summary>
    public class SubscriptionCancellationInvoiceRule : IBusinessRule
    {
        private readonly int _pendingInvoiceCount;
        public Error Error { get; }

        public SubscriptionCancellationInvoiceRule(int pendingInvoiceCount)
        {
            _pendingInvoiceCount = pendingInvoiceCount;
            Error = Error.Custom("Subscription.HasPendingInvoices", 
                "Subscription has pending invoices that must be cancelled first.");
        }

        public bool IsSatisfied()
        {
            // Must not have pending invoices
            return _pendingInvoiceCount == 0;
        }
    }
}
