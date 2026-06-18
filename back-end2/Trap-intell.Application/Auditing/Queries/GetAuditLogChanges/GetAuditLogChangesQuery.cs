using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Auditing.Queries.GetAuditLogChanges;

public sealed record AuditChangeDto(
    string PropertyName,
    string? OldValue,
    string? NewValue
);

public sealed record GetAuditLogChangesQuery(
    Guid OrganizationId,
    Guid AuditTrailId
) : IRequest<Result<IEnumerable<AuditChangeDto>>>;
