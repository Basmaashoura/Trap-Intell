using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Application.Subscriptions.Queries.GetCurrentOrganizationSubscription;

internal sealed class GetCurrentOrganizationSubscriptionQueryHandler : IRequestHandler<GetCurrentOrganizationSubscriptionQuery, Result<SubscriptionSummaryDto>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetCurrentOrganizationSubscriptionQueryHandler(ISubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<Result<SubscriptionSummaryDto>> Handle(GetCurrentOrganizationSubscriptionQuery request, CancellationToken cancellationToken)
    {
        if (request.OrganizationId == Guid.Empty)
        {
            return Result.Failure<SubscriptionSummaryDto>(SubscriptionErrors.InvalidOrganization);
        }

        var subscription = await _subscriptionRepository.GetByOrganizationIdAsync(request.OrganizationId, cancellationToken);
        if (subscription is null)
        {
            return Result.Failure<SubscriptionSummaryDto>(SubscriptionErrors.SubscriptionNotFound);
        }

        var dto = new SubscriptionSummaryDto(
            subscription.Id,
            subscription.OrganizationId,
            subscription.PlanId,
            subscription.Status,
            subscription.BillingCycle,
            subscription.BillingInfo.TotalBilled,
            subscription.BillingInfo.DiscountApplied,
            subscription.Period.StartDate,
            subscription.Period.EndDate,
            subscription.Period.RenewalDate,
            subscription.IsAutoRenew,
            subscription.CurrentUsage.HoneypotsUsed,
            subscription.CurrentUsage.StorageUsedGb,
            subscription.CurrentUsage.OverageCharges,
            subscription.UpdatedAt);

        return Result.Success(dto);
    }
}
