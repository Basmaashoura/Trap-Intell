using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Honeypots;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Domain.Subscriptions.Entities;

namespace Trap_Intel.Application.Honeypots.Events;

internal sealed class HoneypotSubscriptionUsageSynchronizationHandler :
    INotificationHandler<HoneypotCreatedEvent>,
    INotificationHandler<HoneypotTerminatedEvent>,
    INotificationHandler<HoneypotStorageUpdatedEvent>,
    INotificationHandler<HoneypotStatusChangedEvent>
{
    private readonly IHoneypotRepository _honeypotRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HoneypotSubscriptionUsageSynchronizationHandler> _logger;

    public HoneypotSubscriptionUsageSynchronizationHandler(
        IHoneypotRepository honeypotRepository,
        ISubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork,
        ILogger<HoneypotSubscriptionUsageSynchronizationHandler> logger)
    {
        _honeypotRepository = honeypotRepository;
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task Handle(HoneypotCreatedEvent notification, CancellationToken cancellationToken)
    {
        return SynchronizeBySubscriptionAsync(notification.SubscriptionId, cancellationToken);
    }

    public Task Handle(HoneypotTerminatedEvent notification, CancellationToken cancellationToken)
    {
        return SynchronizeBySubscriptionAsync(notification.SubscriptionId, cancellationToken);
    }

    public Task Handle(HoneypotStorageUpdatedEvent notification, CancellationToken cancellationToken)
    {
        return SynchronizeByHoneypotAsync(notification.HoneypotId, cancellationToken);
    }

    public Task Handle(HoneypotStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        return SynchronizeByHoneypotAsync(notification.HoneypotId, cancellationToken);
    }

    private async Task SynchronizeByHoneypotAsync(Guid honeypotId, CancellationToken cancellationToken)
    {
        if (honeypotId == Guid.Empty || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var honeypot = await _honeypotRepository.GetByIdAsync(honeypotId, cancellationToken);
        if (honeypot is null)
        {
            _logger.LogWarning(
                "Skipping usage synchronization because honeypot was not found. HoneypotId={HoneypotId}",
                honeypotId);
            return;
        }

        await SynchronizeBySubscriptionAsync(honeypot.SubscriptionId, cancellationToken);
    }

    private async Task SynchronizeBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        if (subscriptionId == Guid.Empty || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription is null)
        {
            _logger.LogWarning(
                "Skipping usage synchronization because subscription was not found. SubscriptionId={SubscriptionId}",
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
            eventsCaptured: 0,
            periodType: UsagePeriodType.OnDemand);

        if (snapshotResult.IsFailure)
        {
            _logger.LogWarning(
                "Subscription usage snapshot synchronization failed. SubscriptionId={SubscriptionId}, ErrorCode={ErrorCode}",
                subscriptionId,
                snapshotResult.Errors.FirstOrDefault()?.Code ?? "n/a");
            return;
        }

        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
