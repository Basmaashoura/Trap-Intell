using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Application.Plans.Commands.ManagePlanLifecycle;

internal sealed class ManagePlanLifecycleCommandHandler : IRequestHandler<ManagePlanLifecycleCommand, Result>
{
    private readonly IPlanRepository _planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ManagePlanLifecycleCommandHandler(
        IPlanRepository planRepository,
        ISubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork)
    {
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ManagePlanLifecycleCommand request, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan is null)
        {
            return Result.Failure(PlanErrors.PlanNotFound);
        }

        var lifecycleResult = request.Action switch
        {
            PlanLifecycleAction.Activate => Activate(plan),
            PlanLifecycleAction.Deactivate => await DeactivateAsync(plan, cancellationToken),
            _ => Result.Failure(Error.Custom("Plan.UnsupportedAction", "Unsupported plan lifecycle action."))
        };

        if (lifecycleResult.IsFailure)
        {
            return lifecycleResult;
        }

        await _planRepository.UpdateAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static Result Activate(Plan plan)
    {
        plan.Activate();
        return Result.Success();
    }

    private async Task<Result> DeactivateAsync(Plan plan, CancellationToken cancellationToken)
    {
        var activeSubscriptions = await _subscriptionRepository.CountByPlanAsync(
            plan.Id,
            SubscriptionStatus.Active,
            cancellationToken);

        if (activeSubscriptions > 0)
        {
            return Result.Failure(PlanErrors.CannotDeactivateWithActiveSubscriptions);
        }

        plan.Deactivate();
        return Result.Success();
    }
}
