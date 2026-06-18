using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Application.Auditing.Queries.GetUnacknowledgedCriticalAuditCount;

internal sealed class GetUnacknowledgedCriticalAuditCountQueryHandler : IRequestHandler<GetUnacknowledgedCriticalAuditCountQuery, Result<int>>
{
    private readonly IAuditTrailRepository _auditRepository;

    public GetUnacknowledgedCriticalAuditCountQueryHandler(IAuditTrailRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<Result<int>> Handle(GetUnacknowledgedCriticalAuditCountQuery request, CancellationToken cancellationToken)
    {
        var count = await _auditRepository.CountUnacknowledgedCriticalEntriesAsync(request.OrganizationId);
        return Result.Success(count);
    }
}
