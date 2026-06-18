using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Organizations;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Application.Subscriptions.Commands.CreateSubscription;

internal sealed class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, Result<Guid>>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IPlanRepository _planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSubscriptionCommandHandler(
        IOrganizationRepository organizationRepository,
        IPlanRepository planRepository,
        ISubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork)
    {
        _organizationRepository = organizationRepository;
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null)
        {
            return Result.Failure<Guid>(OrganizationErrors.OrganizationNotFound);
        }

        var currentSubscription = await _subscriptionRepository.GetByOrganizationIdAsync(request.OrganizationId, cancellationToken);
        if (currentSubscription is not null &&
            (currentSubscription.Status == SubscriptionStatus.Active || currentSubscription.Status == SubscriptionStatus.Trial))
        {
            return Result.Failure<Guid>(Error.Custom(
                "Subscription.AlreadyExists",
                "Organization already has an active subscription."));
        }

        var creationService = new CreateSubscriptionService(_planRepository, _subscriptionRepository);

        var subscriptionResult = request.IsTrial
            ? await creationService.CreateTrialAsync(
                request.OrganizationId,
                request.PlanId,
                request.TrialDays,
                cancellationToken)
            : await creationService.CreateAsync(
                request.OrganizationId,
                request.PlanId,
                request.BillingCycle,
                cancellationToken);

        if (subscriptionResult.IsFailure)
        {
            return Result.Failure<Guid>(subscriptionResult.Errors);
        }

        var subscription = subscriptionResult.Value;

        if (!request.IsTrial && request.ActivateImmediately)
        {
            var activationResult = subscription.Activate();
            if (activationResult.IsFailure)
            {
                return Result.Failure<Guid>(activationResult.Errors);
            }
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<Guid>(SubscriptionErrors.ConcurrencyConflict);
        }

        return Result.Success(subscription.Id);
    }
}
