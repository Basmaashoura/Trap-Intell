using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Auditing.Queries.GetUnacknowledgedCriticalAuditCount;

public sealed record GetUnacknowledgedCriticalAuditCountQuery(
    Guid OrganizationId
) : IRequest<Result<int>>;
