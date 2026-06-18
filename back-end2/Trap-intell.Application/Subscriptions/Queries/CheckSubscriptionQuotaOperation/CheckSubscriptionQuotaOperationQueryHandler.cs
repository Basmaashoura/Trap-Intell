using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Domain.Subscriptions.Services;

namespace Trap_Intel.Application.Subscriptions.Queries.CheckSubscriptionQuotaOperation;

internal sealed class CheckSubscriptionQuotaOperationQueryHandler : IRequestHandler<CheckSubscriptionQuotaOperationQuery, Result<SubscriptionQuotaOperationCheckDto>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public CheckSubscriptionQuotaOperationQueryHandler(ISubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<Result<SubscriptionQuotaOperationCheckDto>> Handle(CheckSubscriptionQuotaOperationQuery request, CancellationToken cancellationToken)
    {
        if (request.AdditionalHoneypots < 0)
        {
            return Result.Failure<SubscriptionQuotaOperationCheckDto>(QuotaErrors.InvalidHoneypotCount);
        }

        if (request.AdditionalStorageGb < 0)
        {
            return Result.Failure<SubscriptionQuotaOperationCheckDto>(QuotaErrors.InvalidStorageValue);
        }

        var subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null || subscription.OrganizationId != request.OrganizationId)
        {
            return Result.Failure<SubscriptionQuotaOperationCheckDto>(SubscriptionErrors.SubscriptionNotFound);
        }

        if (subscription.Quota is null)
        {
            return Result.Failure<SubscriptionQuotaOperationCheckDto>(QuotaErrors.NoActiveQuota);
        }

        var usage = subscription.CurrentUsage;
        var quota = new SubscriptionQuota(
            subscription.Quota.MaxHoneypots,
            subscription.Quota.MaxStorageGb,
            subscription.Quota.MaxMonthlyApiCalls,
            subscription.Quota.MaxUsers,
            subscription.Quota.HardLimitEnforced);

        var checker = new QuotaChecker();
        var validator = new QuotaOperationValidator(checker);

        var wouldExceed = validator.WouldExceedQuota(
            usage,
            quota,
            request.AdditionalHoneypots,
            request.AdditionalStorageGb);

        var projectedHoneypots = usage.HoneypotsUsed + request.AdditionalHoneypots;
        var projectedStorage = usage.StorageUsedGb + request.AdditionalStorageGb;

        var isAllowed = !wouldExceed || !quota.HardLimitEnforced;
        var message = isAllowed
            ? "Operation is allowed for requested quota usage."
            : "Operation would exceed hard quota limits and is blocked.";

        var dto = new SubscriptionQuotaOperationCheckDto(
            subscription.Id,
            usage.HoneypotsUsed,
            quota.MaxHoneypots,
            usage.StorageUsedGb,
            quota.MaxStorageGb,
            request.AdditionalHoneypots,
            request.AdditionalStorageGb,
            projectedHoneypots,
            projectedStorage,
            quota.HardLimitEnforced,
            wouldExceed,
            isAllowed,
            message);

        return Result.Success(dto);
    }
}
