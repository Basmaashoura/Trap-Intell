using MediatR;
using System;
using System.Collections.Generic;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts.Enums;

namespace Trap_Intel.Application.Alerts.Queries.GetAlerts;

public sealed record GetAlertsQuery(
    Guid OrganizationId,
    AlertStatus? Status = null,
    AlertSeverity? Severity = null,
    AlertType? Type = null,
    Guid? AssignedUserId = null,
    GlobalQueryOptions? Query = null
) : IRequest<Result<PagedResult<AlertDto>>>;
