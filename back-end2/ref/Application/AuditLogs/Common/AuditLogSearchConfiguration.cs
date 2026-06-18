using Application.Common.Filtering;
using Domain.AuditLogs;

namespace Application.AuditLogs.Common;

/// <summary>
/// Search configuration for AuditLog entity.
/// </summary>
public sealed class AuditLogSearchConfiguration : SearchConfiguration<AuditLog>
{
    public static readonly AuditLogSearchConfiguration Instance = new();

    private AuditLogSearchConfiguration()
    {
        AddSearchableField(a => a.UserEmail ?? string.Empty);
        AddSearchableField(a => a.EntityType);
        AddSearchableField(a => a.EntityName ?? string.Empty);
        AddSearchableField(a => a.Description ?? string.Empty);
    }
}
