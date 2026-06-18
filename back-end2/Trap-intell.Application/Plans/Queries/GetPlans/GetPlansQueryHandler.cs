using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Application.Plans.Queries.GetPlans;

internal sealed class GetPlansQueryHandler : IRequestHandler<GetPlansQuery, Result<IReadOnlyList<PlanSummaryDto>>>
{
    private readonly IPlanRepository _planRepository;

    public GetPlansQueryHandler(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<Result<IReadOnlyList<PlanSummaryDto>>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<Plan> plans;

        if (request.Type.HasValue)
        {
            plans = await _planRepository.GetByTypeAsync(request.Type.Value, cancellationToken);
        }
        else if (request.IncludeInactive)
        {
            plans = await _planRepository.GetAllAsync(cancellationToken);
        }
        else
        {
            plans = await _planRepository.GetAllActiveAsync(cancellationToken);
        }

        var result = plans
            .Where(plan => request.IncludeInactive || plan.IsActive)
            .OrderBy(plan => plan.Name)
            .Select(plan =>
            {
                var monthly = plan.GetPrice(BillingCycle.Monthly);

                return new PlanSummaryDto(
                    plan.Id,
                    plan.Name,
                    plan.Description,
                    plan.Type,
                    plan.IsActive,
                    monthly?.Amount,
                    monthly?.Currency ?? "USD");
            })
            .ToList();

        return Result.Success<IReadOnlyList<PlanSummaryDto>>(result);
    }
}
