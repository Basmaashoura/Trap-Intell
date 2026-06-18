using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Application.Auditing.Commands.TagAuditLog;

public sealed record TagAuditLogCommand(
    Guid OrganizationId,
    Guid AuditTrailId,
    ComplianceStandard Standard
) : IRequest<Result>;
