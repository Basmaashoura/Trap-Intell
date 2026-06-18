using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Auditing;
using System.Collections.Generic;

namespace Trap_Intel.Application.Abstractions.Auditing;

public interface IAuditExportService
{
    Task<byte[]> ExportToCsvAsync(IEnumerable<AuditTrail> logs, CancellationToken cancellationToken = default);
}
