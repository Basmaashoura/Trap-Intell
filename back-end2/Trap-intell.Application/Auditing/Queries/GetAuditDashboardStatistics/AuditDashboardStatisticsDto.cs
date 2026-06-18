using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Application.Auditing.Queries.GetAuditDashboardStatistics;

public record AuditDashboardStatisticsDto
{
    public int TotalEvents { get; init; }
    public int UnacknowledgedCriticalEvents { get; init; }
    public int HighSeverityEvents { get; init; }
    public IReadOnlyList<EventsByResourceDto> TopResourceTypes { get; init; } = new List<EventsByResourceDto>();
    public IReadOnlyList<RecentEventDto> RecentCriticalEvents { get; init; } = new List<RecentEventDto>();
}

public record EventsByResourceDto(AuditResourceType ResourceType, int Count);
public record RecentEventDto(Guid Id, AuditAction Action, AuditResourceType ResourceType, DateTime Timestamp, string? Reason);
