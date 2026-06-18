using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trap_Intel.Domain.Reporting
{
    /// <summary>Repository interface for Report aggregate.</summary>
    public interface IReportRepository
    {
        Task<Report> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task AddAsync(Report report, CancellationToken ct = default);
        Task UpdateAsync(Report report, CancellationToken ct = default);
        Task<IEnumerable<Report>> GetByOrganizationAsync(
            Guid organizationId, CancellationToken ct = default);
        Task<IEnumerable<Report>> GetByTypeAsync(
            ReportType type, CancellationToken ct = default);
    }

    /// <summary>Repository interface for ReportTemplate aggregate.</summary>
    public interface IReportTemplateRepository
    {
        Task<ReportTemplate> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<ReportTemplate> GetByTypeAsync(
            ReportType type, CancellationToken ct = default);
        Task AddAsync(ReportTemplate template, CancellationToken ct = default);
        Task UpdateAsync(ReportTemplate template, CancellationToken ct = default);
        Task<IEnumerable<ReportTemplate>> GetAllAsync(CancellationToken ct = default);
    }

    /// <summary>Repository interface for ReportExport aggregate.</summary>
    public interface IReportExportRepository
    {
        Task<ReportExport> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task AddAsync(ReportExport export, CancellationToken ct = default);
        Task UpdateAsync(ReportExport export, CancellationToken ct = default);
        Task<IEnumerable<ReportExport>> GetByReportAsync(
            Guid reportId, CancellationToken ct = default);
        Task<IEnumerable<ReportExport>> GetByOrganizationAsync(
            Guid organizationId, CancellationToken ct = default);
    }
}
