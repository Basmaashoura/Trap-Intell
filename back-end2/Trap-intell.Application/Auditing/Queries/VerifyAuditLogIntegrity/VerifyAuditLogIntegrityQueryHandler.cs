using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Application.Auditing.Queries.VerifyAuditLogIntegrity;

internal sealed class VerifyAuditLogIntegrityQueryHandler : IRequestHandler<VerifyAuditLogIntegrityQuery, Result<AuditIntegrityResultDto>>
{
    private readonly IAuditTrailRepository _auditTrailRepository;

    public VerifyAuditLogIntegrityQueryHandler(IAuditTrailRepository auditTrailRepository)
    {
        _auditTrailRepository = auditTrailRepository;
    }

    public async Task<Result<AuditIntegrityResultDto>> Handle(VerifyAuditLogIntegrityQuery request, CancellationToken cancellationToken)
    {
        // Limit query bound natively internally or allow bulk scanning via SearchAsync with a large max size
        var logs = await _auditTrailRepository.SearchAsync(
            organizationId: request.OrganizationId,
            startDate: request.StartDate,
            endDate: request.EndDate,
            includeArchived: true,
            pageNumber: 1, // Start broad scan
            pageSize: 50000 // A logical bound, might require pagination for huge enterprise setups
        );

        var tamperedRecords = new List<TamperedRecordDto>();

        foreach (var log in logs)
        {
            var result = log.VerifyIntegrity();
            if (result.IsFailure && result.Errors.Any(e => e.Code == AuditingErrors.TamperedAuditLog.Code))
            {
                tamperedRecords.Add(new TamperedRecordDto(log.Id, log.Timestamp, log.RecordHash ?? string.Empty));
            }
        }

        var report = new AuditIntegrityResultDto(
            TotalChecked: logs.Count,
            TamperedCount: tamperedRecords.Count,
            TamperedRecords: tamperedRecords
        );

        return Result.Success(report);
    }
}
