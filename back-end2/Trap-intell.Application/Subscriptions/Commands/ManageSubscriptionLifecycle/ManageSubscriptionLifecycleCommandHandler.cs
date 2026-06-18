using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Application.Subscriptions.Commands.ManageSubscriptionLifecycle;

internal sealed class ManageSubscriptionLifecycleCommandHandler : IRequestHandler<ManageSubscriptionLifecycleCommand, Result>
{
    private const decimal MaxPersistableStorageGb = 99999999999999.9999m;

    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ManageSubscriptionLifecycleCommandHandler(
        ISubscriptionRepository subscriptionRepository,
        IPlanRepository planRepository,
        IUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ManageSubscriptionLifecycleCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null || subscription.OrganizationId != request.OrganizationId)
        {
            return Result.Failure(SubscriptionErrors.SubscriptionNotFound);
        }

        if (IsNoOp(subscription, request.Action))
        {
            return Result.Success();
        }

        var lifecycleResult = request.Action switch
        {
            SubscriptionLifecycleAction.Activate => subscription.Activate(),
            SubscriptionLifecycleAction.Suspend => subscription.Suspend(),
            SubscriptionLifecycleAction.Cancel => Cancel(subscription, request.Reason),
            SubscriptionLifecycleAction.EnableAutoRenew => SetAutoRenew(subscription, true),
            SubscriptionLifecycleAction.DisableAutoRenew => SetAutoRenew(subscription, false),
            SubscriptionLifecycleAction.ChangePlan => await ChangePlanAsync(subscription, request.NewPlanId, cancellationToken),
            SubscriptionLifecycleAction.Renew => Renew(subscription, request.RenewalEndDate),
            SubscriptionLifecycleAction.ScheduleCancellation => ScheduleCancellation(subscription, request.Reason),
            _ => Result.Failure(Error.Custom("Subscription.UnsupportedAction", "Unsupported subscription lifecycle action."))
        };

        if (lifecycleResult.IsFailure)
        {
            return lifecycleResult;
        }

        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure(SubscriptionErrors.ConcurrencyConflict);
        }

        return Result.Success();
    }

    private static bool IsNoOp(Subscription subscription, SubscriptionLifecycleAction action)
    {
        return action switch
        {
            SubscriptionLifecycleAction.Activate => subscription.Status == SubscriptionStatus.Active,
            SubscriptionLifecycleAction.Suspend => subscription.Status == SubscriptionStatus.Suspended,
            SubscriptionLifecycleAction.Cancel => subscription.Status == SubscriptionStatus.Cancelled,
            SubscriptionLifecycleAction.EnableAutoRenew => subscription.IsAutoRenew && !subscription.IsCancellationScheduled,
            SubscriptionLifecycleAction.DisableAutoRenew => !subscription.IsAutoRenew,
            SubscriptionLifecycleAction.ScheduleCancellation => subscription.IsCancellationScheduled,
            _ => false
        };
    }

    private static Result Cancel(Subscription subscription, string? reason)
    {
        var finalReason = string.IsNullOrWhiteSpace(reason)
            ? "Subscription cancelled by administrator."
            : reason.Trim();

        return subscription.Cancel(finalReason);
    }

    private static Result SetAutoRenew(Subscription subscription, bool enabled)
    {
        if (enabled)
        {
            subscription.ClearScheduledCancellation();
            subscription.EnableAutoRenewal();
        }
        else
        {
            subscription.DisableAutoRenewal();
        }

        return Result.Success();
    }

    private static Result ScheduleCancellation(Subscription subscription, string? reason)
    {
        if (subscription.Status == SubscriptionStatus.Cancelled)
        {
            return Result.Failure(SubscriptionErrors.SubscriptionAlreadyCancelled);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(SubscriptionErrors.InvalidCancellationReason);
        }

        subscription.ScheduleCancellationAtPeriodEnd(reason.Trim());
        return Result.Success();
    }

    private static Result Renew(Subscription subscription, DateTime? requestedRenewalEndDate)
    {
        if (subscription.Status == SubscriptionStatus.Cancelled)
        {
            return Result.Failure(SubscriptionErrors.CannotRenewCancelledSubscription);
        }

        var currentEndDate = subscription.Period.EndDate;
        if (!currentEndDate.HasValue)
        {
            return Result.Failure(SubscriptionErrors.SubscriptionPeriodInvalid);
        }

        if (!requestedRenewalEndDate.HasValue)
        {
            return Result.Failure(SubscriptionErrors.InvalidDates);
        }

        var renewalStart = currentEndDate.Value;
        var renewalEnd = requestedRenewalEndDate.Value;

        if (renewalEnd <= renewalStart)
        {
            return Result.Failure(SubscriptionErrors.InvalidDates);
        }

        var renewalPeriod = new SubscriptionPeriod(
            renewalStart,
            renewalEnd,
            renewalEnd);

        return subscription.Renew(renewalPeriod);
    }

    private async Task<Result> ChangePlanAsync(Subscription subscription, Guid? newPlanId, CancellationToken cancellationToken)
    {
        if (!newPlanId.HasValue || newPlanId.Value == Guid.Empty)
        {
            return Result.Failure(PlanErrors.PlanNotFound);
        }

        var newPlan = await _planRepository.GetByIdAsync(newPlanId.Value, cancellationToken);
        if (newPlan is null)
        {
            return Result.Failure(PlanErrors.PlanNotFound);
        }

        if (!newPlan.IsActive)
        {
            return Result.Failure(Error.Custom("Plan.Inactive", "Cannot assign an inactive plan to a subscription."));
        }

        var newPrice = newPlan.GetPrice(subscription.BillingCycle);
        if (newPrice is null)
        {
            return Result.Failure(PlanErrors.PricingNotFound);
        }

        var usageFitResult = ValidateUsageFitsTargetPlan(subscription, newPlan);
        if (usageFitResult.IsFailure)
        {
            return usageFitResult;
        }

        var changePlanResult = subscription.ChangePlan(newPlan.Id, newPrice.Amount);
        if (changePlanResult.IsFailure)
        {
            return changePlanResult;
        }

        return UpdateSubscriptionQuotaFromPlan(subscription, newPlan);
    }

    private static Result ValidateUsageFitsTargetPlan(Subscription subscription, Plan targetPlan)
    {
        var targetLimits = ResolveEffectiveQuotaLimits(targetPlan.QuotaDefinition);
        var usage = subscription.GetQuotaUsageSummary();

        var exceedsTargetLimits =
            usage.CurrentHoneypots > targetLimits.MaxHoneypots ||
            usage.CurrentStorageGb > targetLimits.MaxStorageGb ||
            usage.CurrentApiCalls > targetLimits.MaxMonthlyApiCalls;

        if (!exceedsTargetLimits)
        {
            return Result.Success();
        }

        var currentQuota = subscription.Quota;
        var isQuotaDowngrade =
            currentQuota is null ||
            targetLimits.MaxHoneypots < currentQuota.MaxHoneypots ||
            targetLimits.MaxStorageGb < currentQuota.MaxStorageGb ||
            targetLimits.MaxMonthlyApiCalls < currentQuota.MaxMonthlyApiCalls ||
            targetLimits.MaxUsers < currentQuota.MaxUsers;

        return isQuotaDowngrade
            ? Result.Failure(SubscriptionErrors.CannotDowngradeWithHighUsage)
            : Result.Failure(SubscriptionErrors.SubscriptionPlanChangeNotAllowed);
    }

    private static QuotaLimits ResolveEffectiveQuotaLimits(PlanQuotaDefinition? quotaDefinition)
    {
        var maxHoneypots = quotaDefinition is null || quotaDefinition.MaxHoneypots <= 0
            ? int.MaxValue
            : quotaDefinition.MaxHoneypots;

        var maxStorageGb = quotaDefinition is null || quotaDefinition.MaxStorageGb <= 0
            ? MaxPersistableStorageGb
            : Math.Min(quotaDefinition.MaxStorageGb, MaxPersistableStorageGb);

        var maxMonthlyApiCalls = quotaDefinition is null || quotaDefinition.MaxMonthlyApiCalls <= 0
            ? int.MaxValue
            : quotaDefinition.MaxMonthlyApiCalls;

        var maxUsers = quotaDefinition is null || quotaDefinition.MaxUsers <= 0
            ? int.MaxValue
            : quotaDefinition.MaxUsers;

        var hardLimitEnforced = quotaDefinition?.HardLimitEnforced ?? false;

        var overageHoneypotRate = quotaDefinition is null || quotaDefinition.OverageHoneypotRate < 0
            ? 0m
            : quotaDefinition.OverageHoneypotRate;

        var overageStorageRatePerGb = quotaDefinition is null || quotaDefinition.OverageStorageRatePerGb < 0
            ? 0m
            : quotaDefinition.OverageStorageRatePerGb;

        return new QuotaLimits(
            maxHoneypots,
            maxStorageGb,
            maxMonthlyApiCalls,
            maxUsers,
            hardLimitEnforced,
            overageHoneypotRate,
            overageStorageRatePerGb);
    }

    private static Result UpdateSubscriptionQuotaFromPlan(Subscription subscription, Plan plan)
    {
        var limits = ResolveEffectiveQuotaLimits(plan.QuotaDefinition);

        return subscription.UpdateQuota(
            limits.MaxHoneypots,
            limits.MaxStorageGb,
            limits.MaxMonthlyApiCalls,
            limits.MaxUsers,
            limits.HardLimitEnforced,
            limits.OverageHoneypotRate,
            limits.OverageStorageRatePerGb,
            plan.Id);
    }

    private readonly record struct QuotaLimits(
        int MaxHoneypots,
        decimal MaxStorageGb,
        int MaxMonthlyApiCalls,
        int MaxUsers,
        bool HardLimitEnforced,
        decimal OverageHoneypotRate,
        decimal OverageStorageRatePerGb);
}
