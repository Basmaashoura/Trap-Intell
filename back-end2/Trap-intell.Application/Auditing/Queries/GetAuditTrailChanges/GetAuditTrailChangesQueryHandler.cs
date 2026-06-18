using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Application.Auditing.Queries.GetAuditTrailChanges;

internal sealed class GetAuditTrailChangesQueryHandler : IRequestHandler<GetAuditTrailChangesQuery, Result<IEnumerable<AuditChangeDto>>>
{
    private readonly IAuditTrailRepository _auditRepository;

    public GetAuditTrailChangesQueryHandler(IAuditTrailRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<Result<IEnumerable<AuditChangeDto>>> Handle(GetAuditTrailChangesQuery request, CancellationToken cancellationToken)
    {
        var audit = await _auditRepository.GetByIdAsync(request.AuditTrailId);

        // Security: Filter cross-tenant access secretly pretending it doesn't exist
        if (audit is null || audit.OrganizationId != request.OrganizationId)
        {
            return Result.Failure<IEnumerable<AuditChangeDto>>(AuditingErrors.InvalidResourceId);
        }

        var dtos = audit.Changes.Select(c => new AuditChangeDto(
            c.PropertyName,
            c.OldValue,
            c.NewValue
        ));

        return Result.Success(dtos.AsEnumerable());
    }
}
