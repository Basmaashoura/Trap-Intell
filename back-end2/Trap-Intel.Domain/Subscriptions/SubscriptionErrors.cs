using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Subscriptions
{
    /// <summary>
    /// Error codes and factory methods for the Subscriptions domain.
    /// Follows DDD error handling patterns with semantic error codes.
    /// </summary>
    public static class SubscriptionErrors
    {
        // Core errors
        public static readonly Error SubscriptionNotFound = Error.Custom(
            "Subscription.NotFound",
            "The requested subscription does not exist.");

        public static readonly Error InvalidOrganization = Error.Custom(
            "Subscription.InvalidOrganization",
            "Organization ID cannot be empty.");

        public static readonly Error OrganizationNotFound = Error.Custom(
            "Subscription.OrganizationNotFound",
            "The specified organization does not exist.");

        public static readonly Error InvalidPlan = Error.Custom(
            "Subscription.InvalidPlan",
            "Plan ID cannot be empty.");

        public static readonly Error PlanNotFound = Error.Custom(
            "Subscription.PlanNotFound",
            "The specified plan does not exist.");

        // Date validation errors
        public static readonly Error InvalidDates = Error.Custom(
            "Subscription.InvalidDates",
            "Start date must be before end date.");

        public static readonly Error InvalidPeriod = Error.Custom(
            "Subscription.InvalidPeriod",
            "Subscription period cannot be null.");

        public static readonly Error StartDateInPast = Error.Custom(
            "Subscription.StartDateInPast",
            "Subscription start date cannot be in the past.");

        public static readonly Error EndDateInPast = Error.Custom(
            "Subscription.EndDateInPast",
            "Subscription end date cannot be in the past.");

        public static readonly Error RenewalDateInvalid = Error.Custom(
            "Subscription.RenewalDateInvalid",
            "Renewal date must be after end date.");

        public static readonly Error SubscriptionExpired = Error.Custom(
            "Subscription.SubscriptionExpired",
            "Subscription has expired and cannot be modified.");

        // Billing validation errors
        public static readonly Error InvalidBilling = Error.Custom(
            "Subscription.InvalidBilling",
            "Billing information cannot be null.");

        public static readonly Error InvalidBillingCycle = Error.Custom(
            "Subscription.InvalidBillingCycle",
            "Invalid billing cycle specified.");

        public static readonly Error InvalidBillingAmount = Error.Custom(
            "Subscription.InvalidBillingAmount",
            "Billing amount cannot be negative.");

        public static readonly Error InvalidDiscountAmount = Error.Custom(
            "Subscription.InvalidDiscountAmount",
            "Discount amount cannot exceed billing amount.");

        public static readonly Error OverageChargesNegative = Error.Custom(
            "Subscription.OverageChargesNegative",
            "Overage charges cannot be negative.");

        // Usage validation errors
        public static readonly Error InvalidUsage = Error.Custom(
            "Subscription.InvalidUsage",
            "Usage values cannot be negative.");

        public static readonly Error HoneypotsUsageExceeded = Error.Custom(
            "Subscription.HoneypotsUsageExceeded",
            "Honeypots usage has exceeded the plan limit.");

        public static readonly Error StorageUsageExceeded = Error.Custom(
            "Subscription.StorageUsageExceeded",
            "Storage usage has exceeded the plan limit.");

        public static readonly Error UsersUsageExceeded = Error.Custom(
            "Subscription.UsersUsageExceeded",
            "Number of users has exceeded the plan limit.");

        public static readonly Error SubscriptionQuotaExceeded = Error.Custom(
            "Subscription.QuotaExceeded",
            "Subscription quota limit has been exceeded.");

        public static readonly Error SubscriptionUsageWarning = Error.Custom(
            "Subscription.UsageWarning",
            "Subscription usage has reached warning threshold (80%).");

        public static readonly Error SubscriptionHoneypotLimitExceeded = Error.Custom(
            "Subscription.HoneypotLimitExceeded",
            "Maximum honeypots quota reached.");

        public static readonly Error SubscriptionStorageLimitExceeded = Error.Custom(
            "Subscription.StorageLimitExceeded",
            "Maximum storage quota reached.");

        // Status transition errors
        public static readonly Error SubscriptionInvalidStatus = Error.Custom(
            "Subscription.InvalidStatus",
            "The subscription is in an invalid status for this operation.");

        public static readonly Error SubscriptionNotActive = Error.Custom(
            "Subscription.NotActive",
            "The subscription is not active.");

        public static readonly Error SubscriptionAlreadyActive = Error.Custom(
            "Subscription.AlreadyActive",
            "The subscription is already active.");

        public static readonly Error SubscriptionAlreadySuspended = Error.Custom(
            "Subscription.AlreadySuspended",
            "The subscription is already suspended.");

        public static readonly Error SubscriptionAlreadyCancelled = Error.Custom(
            "Subscription.AlreadyCancelled",
            "The subscription is already cancelled.");

        public static readonly Error SubscriptionCannotChangeStatus = Error.Custom(
            "Subscription.CannotChangeStatus",
            "The subscription status cannot be changed in the current state.");

        public static readonly Error SubscriptionPeriodInvalid = Error.Custom(
            "Subscription.PeriodInvalid",
            "The subscription period is invalid.");

        public static readonly Error SubscriptionPeriodExpired = Error.Custom(
            "Subscription.PeriodExpired",
            "The subscription period has expired.");

        public static readonly Error SubscriptionBillingInfoInvalid = Error.Custom(
            "Subscription.BillingInfoInvalid",
            "The subscription billing information is invalid.");

        public static readonly Error InvalidStatusTransition = Error.Custom(
            "Subscription.InvalidStatusTransition",
            "The requested status transition is not allowed.");

        // Cancellation errors
        public static readonly Error SubscriptionNotEligibleForRenewal = Error.Custom(
            "Subscription.NotEligibleForRenewal",
            "Subscription is not eligible for renewal.");

        public static readonly Error SubscriptionCannotRenew = Error.Custom(
            "Subscription.CannotRenew",
            "The subscription cannot be renewed in its current state.");

        public static readonly Error CannotRenewCancelledSubscription = Error.Custom(
            "Subscription.CannotRenewCancelled",
            "Cannot renew a cancelled subscription.");

        public static readonly Error CancellationAlreadyScheduled = Error.Custom(
            "Subscription.CancellationAlreadyScheduled",
            "Cancellation is scheduled for this subscription and must be cleared before renewal.");

        public static readonly Error SubscriptionCannotCancel = Error.Custom(
            "Subscription.CannotCancel",
            "The subscription cannot be cancelled in its current state.");

        public static readonly Error InvalidCancellationReason = Error.Custom(
            "Subscription.InvalidCancellationReason",
            "Cancellation reason cannot be empty.");

        public static readonly Error CancellationReasonTooShort = Error.Custom(
            "Subscription.CancellationReasonTooShort",
            "Cancellation reason must be at least 5 characters long.");

        public static readonly Error CancellationReasonTooLong = Error.Custom(
            "Subscription.CancellationReasonTooLong",
            "Cancellation reason cannot exceed 500 characters.");

        public static readonly Error CannotCancelNonActiveSubscription = Error.Custom(
            "Subscription.CannotCancelNonActiveSubscription",
            "Can only cancel active subscriptions.");

        public static readonly Error RenewalFailed = Error.Custom(
            "Subscription.RenewalFailed",
            "Renewal process failed due to an unknown error.");

        public static readonly Error RenewalNotAllowed = Error.Custom(
            "Subscription.RenewalNotAllowed",
            "This subscription is not allowed to renew.");

        public static readonly Error CancellationFailed = Error.Custom(
            "Subscription.CancellationFailed",
            "Cancellation process failed due to an unknown error.");

        public static readonly Error CancellationNotAllowed = Error.Custom(
            "Subscription.CancellationNotAllowed",
            "This subscription is not allowed to be cancelled.");

        // Payment method errors
        public static readonly Error InvalidPaymentMethod = Error.Custom(
            "Subscription.InvalidPaymentMethod",
            "Payment method ID cannot be empty.");

        public static readonly Error SubscriptionInvalidPaymentMethod = Error.Custom(
            "Subscription.InvalidPaymentMethod",
            "The payment method is not valid for this subscription.");

        public static readonly Error PaymentMethodNotFound = Error.Custom(
            "Subscription.PaymentMethodNotFound",
            "The specified payment method does not exist.");

        public static readonly Error PaymentMethodExpired = Error.Custom(
            "Subscription.PaymentMethodExpired",
            "The specified payment method has expired.");

        public static readonly Error SubscriptionNoPaymentMethod = Error.Custom(
            "Subscription.NoPaymentMethod",
            "No payment method is configured for this subscription.");

        public static readonly Error SubscriptionPaymentFailed = Error.Custom(
            "Subscription.PaymentFailed",
            "Payment processing failed for this subscription.");

        public static readonly Error CannotSetPaymentMethodOnCancelled = Error.Custom(
            "Subscription.CannotSetPaymentMethodOnCancelled",
            "Cannot set payment method on a cancelled subscription.");

        public static readonly Error RenewalFailedInsufficientFunds = Error.Custom(
            "Subscription.RenewalFailedInsufficientFunds",
            "Renewal failed: insufficient funds.");

        public static readonly Error RenewalFailedPaymentMethodInvalid = Error.Custom(
            "Subscription.RenewalFailedPaymentMethodInvalid",
            "Renewal failed: payment method is invalid.");

        // Auto-renewal errors
        public static readonly Error SubscriptionAutoRenewalNotEnabled = Error.Custom(
            "Subscription.AutoRenewalNotEnabled",
            "Auto-renewal is not enabled for this subscription.");

        public static readonly Error SubscriptionAutoRenewalAlreadyEnabled = Error.Custom(
            "Subscription.AutoRenewalAlreadyEnabled",
            "Auto-renewal is already enabled for this subscription.");

        public static readonly Error CannotEnableAutoRenewalOnExpiring = Error.Custom(
            "Subscription.CannotEnableAutoRenewalOnExpiring",
            "Cannot enable auto-renewal for an expiring subscription.");

        public static readonly Error CannotDisableAutoRenewalOnCancelled = Error.Custom(
            "Subscription.CannotDisableAutoRenewalOnCancelled",
            "Cannot disable auto-renewal on a cancelled subscription.");

        // Plan change errors
        public static readonly Error SubscriptionCannotUpgrade = Error.Custom(
            "Subscription.CannotUpgrade",
            "The subscription cannot be upgraded.");

        public static readonly Error SubscriptionCannotDowngrade = Error.Custom(
            "Subscription.CannotDowngrade",
            "The subscription cannot be downgraded.");

        public static readonly Error SubscriptionPlanChangeNotAllowed = Error.Custom(
            "Subscription.PlanChangeNotAllowed",
            "Plan change is not allowed for this subscription.");

        public static readonly Error CannotUpgradeExpiredSubscription = Error.Custom(
            "Subscription.CannotUpgradeExpiredSubscription",
            "Cannot upgrade an expired subscription.");

        public static readonly Error CannotDowngradeWithHighUsage = Error.Custom(
            "Subscription.CannotDowngradeWithHighUsage",
            "Cannot downgrade to a plan with insufficient limits for current usage.");

        // Suspension errors
        public static readonly Error SubscriptionSuspensionNotAllowed = Error.Custom(
            "Subscription.SuspensionNotAllowed",
            "The subscription cannot be suspended.");

        public static readonly Error CannotSuspendCancelled = Error.Custom(
            "Subscription.CannotSuspendCancelled",
            "Cannot suspend a cancelled subscription.");

        public static readonly Error SubscriptionReactivationNotAllowed = Error.Custom(
            "Subscription.ReactivationNotAllowed",
            "The subscription cannot be reactivated.");

        public static readonly Error CannotActivateExpired = Error.Custom(
            "Subscription.CannotActivateExpired",
            "Cannot activate an expired subscription.");

        public static readonly Error CannotCancelExpired = Error.Custom(
            "Subscription.CannotCancelExpired",
            "Cannot cancel an already expired subscription.");

        // General errors
        public static readonly Error InvalidOperation = Error.Custom(
            "Subscription.InvalidOperation",
            "The requested operation is invalid for the current subscription state.");

        public static readonly Error ConcurrencyConflict = Error.Custom(
            "Subscription.ConcurrencyConflict",
            "Subscription was modified by another process. Please refresh and try again.");

        public static readonly Error DuplicateSubscription = Error.Custom(
            "Subscription.DuplicateSubscription",
            "Organization already has an active subscription.");

        public static readonly Error UsageLimitReached = Error.Custom(
            "Subscription.UsageLimitReached",
            "You have reached the usage limit for this subscription.");

        public static readonly Error ExceedsPlanLimit = Error.Custom(
            "Subscription.ExceedsPlanLimit",
            "This action exceeds the plan limits.");

        public static readonly Error InsufficientPermissions = Error.Custom(
            "Subscription.InsufficientPermissions",
            "You do not have permission to perform this action.");
    }
}
