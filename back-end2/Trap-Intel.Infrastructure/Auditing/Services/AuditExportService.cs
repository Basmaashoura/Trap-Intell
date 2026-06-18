using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Trap_Intel.Application.Abstractions.Auditing;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Infrastructure.Auditing.Services;

internal sealed class AuditExportService : IAuditExportService
{
    public async Task<byte[]> ExportToCsvAsync(IEnumerable<AuditTrail> logs, CancellationToken cancellationToken = default)
    {
        await using var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream, Encoding.UTF8);

        // Header
        await writer.WriteLineAsync("Id,OrganizationId,Timestamp,Action,Severity,ResourceType,ResourceId,UserId,IpAddress,UserAgent,IsAcknowledged,AcknowledgedBy,AcknowledgedAt,AcknowledgeNotes,IsArchived,ComplianceStandards,ChangesCount,Reason");

        // Rows
        foreach (var log in logs)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var complianceStandards = string.Join("|", log.ComplianceStandards.Select(s => s.ToString()));

            var line = string.Join(",",
                log.Id.ToString(),
                log.OrganizationId.ToString(),
                log.Timestamp.ToString("O", CultureInfo.InvariantCulture),
                log.Action.ToString(),
                log.Severity.ToString(),
                log.ResourceType.ToString(),
                log.ResourceId.ToString(),
                EscapeCsv(log.UserId?.ToString()),
                EscapeCsv(log.IpAddress),
                EscapeCsv(log.UserAgent),
                log.IsAcknowledged.ToString(),
                EscapeCsv(log.AcknowledgedBy?.ToString()),
                EscapeCsv(log.AcknowledgedAt?.ToString("O", CultureInfo.InvariantCulture)),
                EscapeCsv(log.AcknowledgeNotes),
                log.IsArchived.ToString(),
                EscapeCsv(complianceStandards),
                log.Changes.Count.ToString(CultureInfo.InvariantCulture),
                EscapeCsv(log.Reason)
            );

            await writer.WriteLineAsync(line);
        }

        await writer.FlushAsync();
        return memoryStream.ToArray();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        return escaped.Contains(',') || escaped.Contains('\n') || escaped.Contains('\r')
            ? $"\"{escaped}\""
            : escaped;
    }
}
