using System;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Alerts.Queries.GetAlertDashboardStatistics;

public sealed record GetAlertDashboardStatisticsQuery(
    Guid OrganizationId,
    int LastNDays = 30
) : IRequest<Result<AlertDashboardStatisticsDto>>;
