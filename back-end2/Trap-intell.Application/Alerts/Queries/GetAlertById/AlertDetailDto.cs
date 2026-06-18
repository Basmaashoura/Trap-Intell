using System;
using System.Collections.Generic;
using Trap_Intel.Domain.Alerts.Enums;
using Trap_Intel.Application.Alerts.Queries.GetAlerts;

namespace Trap_Intel.Application.Alerts.Queries.GetAlertById;

public record AlertDetailDto(
    Guid Id,
    AlertType Type,
    AlertSeverity Severity,
    AlertPriority Priority,
    string Title,
    string Description,
    AlertStatus Status,
    string SourceType,
    string? SourceName,
    Guid? SourceId,
    EscalationLevel EscalationLevel,
    Guid? AssignedToUserId,
    Guid? AcknowledgedByUserId,
    DateTime? AcknowledgedAt,
    Guid? ResolvedByUserId,
    DateTime? ResolvedAt,
    string? Resolution,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ActionDto> Actions,
    IReadOnlyList<CommentDto> Comments);

public record ActionDto(string ActionType, string? Description, Guid PerformedByUserId, DateTime PerformedAt);
public record CommentDto(string Content, Guid AuthorUserId, DateTime CreatedAt, bool IsInternal);
