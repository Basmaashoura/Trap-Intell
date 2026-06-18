using MediatR;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Application.Auditing.Queries.GetAuditLogs;

namespace Trap_Intel.Application.Auditing.Queries.GetCriticalAuditLogs;

public sealed record GetCriticalAuditLogsQuery(
    Guid OrganizationId,
    GlobalQueryOptions? Query = null
) : IRequest<Result<PagedResult<AuditTrailDto>>>;
