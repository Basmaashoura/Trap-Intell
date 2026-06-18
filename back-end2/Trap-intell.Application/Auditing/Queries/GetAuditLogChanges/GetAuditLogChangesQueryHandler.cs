using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Application.Auditing.Queries.GetAuditLogChanges;

internal sealed class GetAuditLogChangesQueryHandler : IRequestHandler<GetAuditLogChangesQuery, Result<IEnumerable<AuditChangeDto>>>
{
    private readonly IAuditTrailRepository _auditRepository;

    public GetAuditLogChangesQueryHandler(IAuditTrailRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<Result<IEnumerable<AuditChangeDto>>> Handle(GetAuditLogChangesQuery request, CancellationToken cancellationToken)
    {
        var audit = await _auditRepository.GetByIdAsync(request.AuditTrailId);

        if (audit == null || audit.OrganizationId != request.OrganizationId)
        {
            return Result.Failure<IEnumerable<AuditChangeDto>>(AuditingErrors.InvalidResourceId);
        }

        var dtos = audit.Changes.Select(c => new AuditChangeDto(
            c.PropertyName,
            c.OldValue,
            c.NewValue
        ));

        return Result.Success(dtos);
    }
}
