using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Application.Plans.Queries.GetPlans;

public sealed record GetPlansQuery(
    PlanType? Type = null,
    bool IncludeInactive = false) : IRequest<Result<IReadOnlyList<PlanSummaryDto>>>;
