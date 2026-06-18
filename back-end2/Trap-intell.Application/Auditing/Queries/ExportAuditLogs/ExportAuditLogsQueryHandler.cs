using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Application.Abstractions.Auditing;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Application.Auditing.Queries.ExportAuditLogs;

internal sealed class ExportAuditLogsQueryHandler : IRequestHandler<ExportAuditLogsQuery, Result<byte[]>>
{
    private readonly IAuditTrailRepository _auditTrailRepository;
    private readonly IAuditExportService _exportService;

    public ExportAuditLogsQueryHandler(IAuditTrailRepository auditTrailRepository, IAuditExportService exportService)
    {
        _auditTrailRepository = auditTrailRepository;
        _exportService = exportService;
    }

    public async Task<Result<byte[]>> Handle(ExportAuditLogsQuery request, CancellationToken cancellationToken)
    {
        // When exporting, it's typical to fetch a large untruncated (or reasonably bounded max) dataset
        var logs = await _auditTrailRepository.SearchAsync(
            request.OrganizationId,
            request.UserId,
            request.Action,
            request.ResourceType,
            request.Severity,
            request.IpAddress,
            request.StartDate,
            request.EndDate,
            request.Standard,
            request.IncludeArchived,
            pageNumber: 1,
            pageSize: 10000 // Limit max to export per API request for performance
        );

        if (logs.Count == 0)
        {
            return Result.Failure<byte[]>(Error.Custom("Export.NoData", "No audit logs found to export."));
        }

        var csvBytes = await _exportService.ExportToCsvAsync(logs, cancellationToken);
        return Result.Success(csvBytes);
    }
}
