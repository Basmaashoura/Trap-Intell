using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Auditing.Queries.GetAuditTrailChanges;

public sealed record AuditChangeDto(
    string PropertyName,
    string? OldValue,
    string? NewValue
);

public sealed record GetAuditTrailChangesQuery(
    Guid OrganizationId,
    Guid AuditTrailId
) : IRequest<Result<IEnumerable<AuditChangeDto>>>;
