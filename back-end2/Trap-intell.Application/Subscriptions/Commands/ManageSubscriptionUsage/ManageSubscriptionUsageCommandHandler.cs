using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Application.Subscriptions.Commands.ManageSubscriptionUsage;

internal sealed class ManageSubscriptionUsageCommandHandler : IRequestHandler<ManageSubscriptionUsageCommand, Result>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ManageSubscriptionUsageCommandHandler(
        ISubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ManageSubscriptionUsageCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null || subscription.OrganizationId != request.OrganizationId)
        {
            return Result.Failure(SubscriptionErrors.SubscriptionNotFound);
        }

        var result = request.Action switch
        {
            SubscriptionUsageAction.RecordSnapshot => subscription.RecordUsageSnapshot(
                request.HoneypotsActive ?? 0,
                request.StorageUsedGb ?? 0,
                request.ApiCallsCount,
                request.ActiveUsers,
                request.EventsCaptured,
                request.PeriodType),

            SubscriptionUsageAction.FinalizeMonthlyUsage => subscription.FinalizeMonthlyUsage(
                request.Year ?? 0,
                request.Month ?? 0),

            SubscriptionUsageAction.MarkMonthlyUsageAsBilled => subscription.MarkMonthlyUsageAsBilled(
                request.Year ?? 0,
                request.Month ?? 0,
                request.InvoiceId ?? Guid.Empty),

            _ => Result.Failure(Error.Custom("Subscription.UnsupportedUsageAction", "Unsupported subscription usage action."))
        };

        if (result.IsFailure)
        {
            return result;
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
}
