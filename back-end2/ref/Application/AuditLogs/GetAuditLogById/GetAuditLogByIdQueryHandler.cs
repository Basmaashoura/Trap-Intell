using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.AuditLogs.Common;
using Domain.AuditLogs;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.AuditLogs.GetAuditLogById;

/// <summary>
/// Handler for GetAuditLogByIdQuery.
/// Returns detailed audit log information including old/new values.
/// </summary>
internal sealed class GetAuditLogByIdQueryHandler : IQueryHandler<GetAuditLogByIdQuery, AuditLogDetailResponse>
{
    private readonly IApplicationDbContext _context;

    public GetAuditLogByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AuditLogDetailResponse>> Handle(
        GetAuditLogByIdQuery query,
        CancellationToken cancellationToken)
    {
        AuditLogDetailResponse? auditLog = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.Id == query.AuditLogId)
            .Select(a => new AuditLogDetailResponse
            {
                Id = a.Id,
                UserId = a.UserId,
                UserEmail = a.UserEmail,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                EntityName = a.EntityName,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                Description = a.Description,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                Timestamp = a.Timestamp,
                CorrelationId = a.CorrelationId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (auditLog is null)
        {
            return Result.Failure<AuditLogDetailResponse>(AuditLogErrors.NotFound(query.AuditLogId));
        }

        return Result.Success(auditLog);
    }
}
