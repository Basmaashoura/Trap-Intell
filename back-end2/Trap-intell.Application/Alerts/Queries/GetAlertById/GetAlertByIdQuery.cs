using System;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Alerts.Queries.GetAlertById;

public sealed record GetAlertByIdQuery(
    Guid OrganizationId,
    Guid AlertId
) : IRequest<Result<AlertDetailDto>>;
