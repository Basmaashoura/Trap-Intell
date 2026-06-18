using System;
using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Application.Auditing.Queries.ExportAuditLogs;

public record ExportAuditLogsQuery(
    Guid OrganizationId,
    Guid? UserId = null,
    AuditAction? Action = null,
    AuditResourceType? ResourceType = null,
    AuditSeverity? Severity = null,
    string? IpAddress = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    ComplianceStandard? Standard = null,
    bool IncludeArchived = false) : IRequest<Result<byte[]>>;
