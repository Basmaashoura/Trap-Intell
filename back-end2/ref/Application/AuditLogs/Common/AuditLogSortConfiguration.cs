using Application.Common.Sorting;
using Domain.AuditLogs;

namespace Application.AuditLogs.Common;

/// <summary>
/// Sorting configuration for AuditLog entity.
/// </summary>
public sealed class AuditLogSortConfiguration : SortConfiguration<AuditLog>
{
    public static readonly AuditLogSortConfiguration Instance = new();

    public override string DefaultSortField => "TIMESTAMP";

    private AuditLogSortConfiguration()
    {
        AddSortableField("Timestamp", a => a.Timestamp);
        AddSortableField("Action", a => a.Action);
        AddSortableField("EntityType", a => a.EntityType);
        AddSortableField("UserEmail", a => a.UserEmail ?? string.Empty);
    }
}
