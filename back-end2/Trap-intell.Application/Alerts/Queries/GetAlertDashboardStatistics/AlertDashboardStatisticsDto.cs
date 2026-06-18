using System.Collections.Generic;

namespace Trap_Intel.Application.Alerts.Queries.GetAlertDashboardStatistics;

public record AlertDashboardStatisticsDto(
    int TotalActiveAlerts,
    int UnacknowledgedAlerts,
    int CriticalUnresolvedAlerts,
    int EscalatedAlerts,
    int FalsePositivesLastNDays,
    IReadOnlyList<AlertTrendDto> AlertsByType,
    IReadOnlyList<AlertTrendDto> AlertsBySeverity);

public record AlertTrendDto(string Category, int Count);
