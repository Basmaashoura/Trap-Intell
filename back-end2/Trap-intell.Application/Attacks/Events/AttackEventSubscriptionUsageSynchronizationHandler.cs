using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Attacks.Events;
using Trap_Intel.Domain.Honeypots;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Domain.Subscriptions.Entities;

namespace Trap_Intel.Application.Attacks.Events;

internal sealed class AttackEventSubscriptionUsageSynchronizationHandler :
    INotificationHandler<AttackEventReceivedEvent>
{
    private readonly IHoneypotRepository _honeypotRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AttackEventSubscriptionUsageSynchronizationHandler> _logger;

    public AttackEventSubscriptionUsageSynchronizationHandler(
        IHoneypotRepository honeypotRepository,
        ISubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork,
        ILogger<AttackEventSubscriptionUsageSynchronizationHandler> logger)
    {
        _honeypotRepository = honeypotRepository;
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(AttackEventReceivedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.HoneypotId == Guid.Empty || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var honeypot = await _honeypotRepository.GetByIdAsync(notification.HoneypotId, cancellationToken);
        if (honeypot is null)
        {
            _logger.LogWarning(
                "Skipping attack usage synchronization because honeypot was not found. HoneypotId={HoneypotId}, AttackEventId={AttackEventId}",
                notification.HoneypotId,
                notification.AttackEventId);
            return;
        }

        if (honeypot.OrganizationId != notification.OrganizationId)
        {
            _logger.LogWarning(
                "Skipping attack usage synchronization due to organization mismatch. HoneypotId={HoneypotId}, HoneypotOrganizationId={HoneypotOrgId}, EventOrganizationId={EventOrgId}, AttackEventId={AttackEventId}",
                notification.HoneypotId,
                honeypot.OrganizationId,
                notification.OrganizationId,
                notification.AttackEventId);
            return;
        }

        await SynchronizeSubscriptionUsageAsync(honeypot.SubscriptionId, cancellationToken);
    }

    private async Task SynchronizeSubscriptionUsageAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        if (subscriptionId == Guid.Empty)
        {
            return;
        }

        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription is null)
        {
            _logger.LogWarning(
                "Skipping attack usage synchronization because subscription was not found. SubscriptionId={SubscriptionId}",
                subscriptionId);
            return;
        }

        var honeypots = await _honeypotRepository.GetBySubscriptionAsync(subscriptionId, cancellationToken);

        var activeHoneypots = honeypots.Count(h =>
            h.Status == HoneypotStatus.Active ||
            h.Status == HoneypotStatus.Paused);

        var storageUsedGb = honeypots.Sum(h => h.Health.StorageUsedGb);

        var snapshotResult = subscription.RecordUsageSnapshot(
            honeypotsActive: activeHoneypots,
            storageUsedGb: storageUsedGb,
            apiCallsCount: 0,
            activeUsers: 0,
            eventsCaptured: 1,
            periodType: UsagePeriodType.OnDemand);

        if (snapshotResult.IsFailure)
        {
            _logger.LogWarning(
                "Attack usage synchronization failed for subscription {SubscriptionId}. ErrorCode={ErrorCode}",
                subscriptionId,
                snapshotResult.Errors.FirstOrDefault()?.Code ?? "n/a");
            return;
        }

        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
