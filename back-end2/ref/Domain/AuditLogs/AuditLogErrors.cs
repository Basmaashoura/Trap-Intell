using SharedKernel;

namespace Domain.AuditLogs;

public static class AuditLogErrors
{
    public static Error NotFound(Guid auditLogId) => Error.NotFound(
        "AuditLogs.NotFound",
        $"The audit log with Id = '{auditLogId}' was not found");
}
