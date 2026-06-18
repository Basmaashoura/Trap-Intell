using Application.Abstractions.Messaging;
using Application.AuditLogs.Common;

namespace Application.AuditLogs.GetAuditLogById;

/// <summary>
/// Query to get an audit log by its ID.
/// </summary>
/// <param name="AuditLogId">The ID of the audit log to retrieve.</param>
public sealed record GetAuditLogByIdQuery(Guid AuditLogId) : IQuery<AuditLogDetailResponse>;
