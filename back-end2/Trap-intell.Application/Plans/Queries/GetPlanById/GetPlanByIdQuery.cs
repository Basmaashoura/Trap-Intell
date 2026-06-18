using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Plans.Queries.GetPlanById;

public sealed record GetPlanByIdQuery(Guid PlanId) : IRequest<Result<PlanDetailDto>>;
