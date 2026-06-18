using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Application.Billing.Services;

internal sealed class PostPaymentSubscriptionRenewalService : IPostPaymentSubscriptionRenewalService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<PostPaymentSubscriptionRenewalService> _logger;

    public PostPaymentSubscriptionRenewalService(
        ISubscriptionRepository subscriptionRepository,
        ILogger<PostPaymentSubscriptionRenewalService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }

    public async Task TryRenewAfterPaidInvoiceAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        if (invoice.Status != InvoiceStatus.Paid)
        {
            return;
        }

        var subscription = await _subscriptionRepository.GetByIdAsync(invoice.SubscriptionId, cancellationToken);
        if (subscription is null)
        {
            _logger.LogWarning(
                "Skipping post-payment renewal because subscription was not found. InvoiceId={InvoiceId}, SubscriptionId={SubscriptionId}",
                invoice.Id,
                invoice.SubscriptionId);
            return;
        }

        if (!subscription.IsAutoRenew || subscription.IsCancellationScheduled)
        {
            return;
        }

        if (subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Suspended)
        {
            return;
        }

        if (!subscription.Period.EndDate.HasValue)
        {
            _logger.LogWarning(
                "Skipping post-payment renewal because subscription period end is missing. SubscriptionId={SubscriptionId}",
                subscription.Id);
            return;
        }

        if (subscription.BillingCycle == BillingCycle.OneTime)
        {
            return;
        }

        var currentPeriodEnd = subscription.Period.EndDate.Value.Date;
        if (invoice.BillingPeriod.EndDate.Date != currentPeriodEnd)
        {
            return;
        }

        var nextPeriod = BuildNextPeriod(subscription.BillingCycle, subscription.Period.EndDate.Value);
        var renewResult = subscription.Renew(nextPeriod);
        if (renewResult.IsFailure)
        {
            var errorCode = renewResult.Errors.Count > 0 ? renewResult.Errors[0].Code : "Subscription.RenewalFailed";
            _logger.LogWarning(
                "Post-payment renewal failed. SubscriptionId={SubscriptionId}, InvoiceId={InvoiceId}, Error={ErrorCode}",
                subscription.Id,
                invoice.Id,
                errorCode);
            return;
        }

        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
    }

    private static SubscriptionPeriod BuildNextPeriod(BillingCycle billingCycle, DateTime previousPeriodEnd)
    {
        var nextStart = previousPeriodEnd;
        var nextEnd = billingCycle switch
        {
            BillingCycle.Monthly => nextStart.AddMonths(1),
            BillingCycle.Quarterly => nextStart.AddMonths(3),
            BillingCycle.Annually => nextStart.AddYears(1),
            _ => nextStart
        };

        return new SubscriptionPeriod(
            StartDate: nextStart,
            EndDate: nextEnd,
            RenewalDate: nextEnd);
    }
}
