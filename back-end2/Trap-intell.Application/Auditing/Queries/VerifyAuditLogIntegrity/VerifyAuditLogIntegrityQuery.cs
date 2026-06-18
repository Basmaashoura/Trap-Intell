using System;
using System.Collections.Generic;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Auditing.Queries.VerifyAuditLogIntegrity;

public record VerifyAuditLogIntegrityQuery(
    Guid OrganizationId,
    DateTime? StartDate = null,
    DateTime? EndDate = null) : IRequest<Result<AuditIntegrityResultDto>>;

public record AuditIntegrityResultDto(
    int TotalChecked,
    int TamperedCount,
    IReadOnlyList<TamperedRecordDto> TamperedRecords);

public record TamperedRecordDto(
    Guid AuditTrailId,
    DateTime Timestamp,
    string OriginalHash);
