using System;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Auditing.Commands.AcknowledgeAuditLog;

public record AcknowledgeAuditLogCommand(
    Guid OrganizationId,
    Guid AuditTrailId,
    Guid UserId,
    string? Notes = null) : IRequest<Result>;
