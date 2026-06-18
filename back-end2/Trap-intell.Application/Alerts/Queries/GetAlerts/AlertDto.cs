using System;
using Trap_Intel.Domain.Alerts.Enums;

namespace Trap_Intel.Application.Alerts.Queries.GetAlerts;

public record AlertDto(
    Guid Id,
    AlertType Type,
    AlertSeverity Severity,
    AlertPriority Priority,
    string Title,
    AlertStatus Status,
    string SourceType,
    string? SourceName,
    Guid? AssignedToUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
