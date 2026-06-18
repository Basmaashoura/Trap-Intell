using System;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Reporting.Services
{
    /// <summary>
    /// Domain service for generating reports from templates and analysis data.
    /// </summary>
    public class ReportGenerationService
    {
        private readonly IReportRepository _reportRepository;
        private readonly IReportTemplateRepository _templateRepository;

        public ReportGenerationService(
            IReportRepository reportRepository,
            IReportTemplateRepository templateRepository)
        {
            _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        }

        /// <summary>Generate a new report.</summary>
        public async Task<Result<Report>> GenerateAsync(
            Guid organizationId,
            ReportType type,
            ReportTitle title,
            ReportSummary summary,
            KPICollection kpis,
            LogDetails logDetails,
            RecommendationCollection recommendations,
            Guid? userId = null,
            Guid? subscriptionId = null,
            CancellationToken cancellationToken = default)
        {
            // Step 1: Get template for report type
            var template = await _templateRepository.GetByTypeAsync(type, cancellationToken);
            if (template == null)
                return Result.Failure<Report>(ReportingErrors.TemplateNotFound);

            // Step 2: Validate template
            var templateValidation = template.Validate();
            if (templateValidation.IsFailure)
                return Result.Failure<Report>(templateValidation.Errors);

            // Step 3: Create report
            var reportResult = Report.Create(
                organizationId, type, title, summary,
                kpis, logDetails, recommendations,
                userId, subscriptionId);

            if (reportResult.IsFailure)
                return Result.Failure<Report>(reportResult.Errors);

            var report = reportResult.Value;

            // Step 4: Save report
            await _reportRepository.AddAsync(report, cancellationToken);

            return Result.Success(report);
        }
    }

    /// <summary>
    /// Domain service for exporting reports in various formats.
    /// </summary>
    public class ReportExportService
    {
        private readonly IReportExportRepository _exportRepository;
        private readonly IReportRepository _reportRepository;

        public ReportExportService(
            IReportExportRepository exportRepository,
            IReportRepository reportRepository)
        {
            _exportRepository = exportRepository ?? throw new ArgumentNullException(nameof(exportRepository));
            _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
        }

        /// <summary>Export a report in specified format.</summary>
        public async Task<Result<ReportExport>> ExportAsync(
            Guid reportId,
            Guid organizationId,
            Guid userId,
            ReportFormat format,
            CancellationToken cancellationToken = default)
        {
            // Step 1: Validate report exists
            var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken);
            if (report == null)
                return Result.Failure<ReportExport>(ReportingErrors.ReportNotFound);

            // Step 2: Set report format
            var formatResult = report.SetFormat(format);
            if (formatResult.IsFailure)
                return Result.Failure<ReportExport>(formatResult.Errors);

            // Step 3: Create export
            var exportResult = ReportExport.Create(
                reportId, organizationId, userId, format);

            if (exportResult.IsFailure)
                return Result.Failure<ReportExport>(exportResult.Errors);

            var export = exportResult.Value;

            // Step 4: Save export
            await _exportRepository.AddAsync(export, cancellationToken);

            // Step 5: Update report
            await _reportRepository.UpdateAsync(report, cancellationToken);

            return Result.Success(export);
        }

        /// <summary>Mark export as completed with file URL.</summary>
        public async Task<Result> CompleteExportAsync(
            Guid exportId,
            string fileUrl,
            CancellationToken cancellationToken = default)
        {
            var export = await _exportRepository.GetByIdAsync(exportId, cancellationToken);
            if (export == null)
                return Result.Failure(ReportingErrors.ExportNotFound);

            var result = export.MarkAsCompleted(fileUrl);
            if (result.IsFailure)
                return Result.Failure(result.Errors);

            await _exportRepository.UpdateAsync(export, cancellationToken);
            return Result.Success();
        }

        /// <summary>Mark export as failed with error message.</summary>
        public async Task<Result> FailExportAsync(
            Guid exportId,
            string errorMessage,
            CancellationToken cancellationToken = default)
        {
            var export = await _exportRepository.GetByIdAsync(exportId, cancellationToken);
            if (export == null)
                return Result.Failure(ReportingErrors.ExportNotFound);

            var result = export.MarkAsFailed(errorMessage);
            if (result.IsFailure)
                return Result.Failure(result.Errors);

            await _exportRepository.UpdateAsync(export, cancellationToken);
            return Result.Success();
        }
    }

    /// <summary>
    /// Domain service for managing report templates.
    /// </summary>
    public class TemplateManagementService
    {
        private readonly IReportTemplateRepository _templateRepository;

        public TemplateManagementService(IReportTemplateRepository templateRepository)
        {
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        }

        /// <summary>Create a new template.</summary>
        public async Task<Result<ReportTemplate>> CreateTemplateAsync(
            Guid? organizationId,
            Guid createdBy,
            ReportType type,
            TemplateName name,
            TemplateGuidelines guidelines,
            CancellationToken cancellationToken = default)
        {
            var templateResult = ReportTemplate.Create(
                organizationId, createdBy, type, name, guidelines);

            if (templateResult.IsFailure)
                return Result.Failure<ReportTemplate>(templateResult.Errors);

            var template = templateResult.Value;
            await _templateRepository.AddAsync(template, cancellationToken);

            return Result.Success(template);
        }

        /// <summary>Add section to template.</summary>
        public async Task<Result> AddSectionAsync(
            Guid templateId,
            TemplateSection section,
            CancellationToken cancellationToken = default)
        {
            var template = await _templateRepository.GetByIdAsync(templateId, cancellationToken);
            if (template == null)
                return Result.Failure(ReportingErrors.TemplateNotFound);

            var result = template.AddSection(section);
            if (result.IsFailure)
                return Result.Failure(result.Errors);

            await _templateRepository.UpdateAsync(template, cancellationToken);
            return Result.Success();
        }

        /// <summary>Validate template structure.</summary>
        public async Task<Result> ValidateTemplateAsync(
            Guid templateId,
            CancellationToken cancellationToken = default)
        {
            var template = await _templateRepository.GetByIdAsync(templateId, cancellationToken);
            if (template == null)
                return Result.Failure(ReportingErrors.TemplateNotFound);

            return template.Validate();
        }
    }

    /// <summary>
    /// Domain service for managing report lifecycle.
    /// </summary>
    public class ReportManagementService
    {
        private readonly IReportRepository _reportRepository;

        public ReportManagementService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
        }

        /// <summary>Review and approve a report.</summary>
        public async Task<Result> ReviewReportAsync(
            Guid reportId,
            Guid reviewerId,
            string reviewNotes,
            CancellationToken cancellationToken = default)
        {
            var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken);
            if (report == null)
                return Result.Failure(ReportingErrors.ReportNotFound);

            var result = report.Review(reviewerId, reviewNotes);
            if (result.IsFailure)
                return Result.Failure(result.Errors);

            await _reportRepository.UpdateAsync(report, cancellationToken);
            return Result.Success();
        }

        /// <summary>Send a report to recipient.</summary>
        public async Task<Result> SendReportAsync(
            Guid reportId,
            Guid recipientId,
            string recipientEmail,
            CancellationToken cancellationToken = default)
        {
            var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken);
            if (report == null)
                return Result.Failure(ReportingErrors.ReportNotFound);

            var result = report.Send(recipientId, recipientEmail);
            if (result.IsFailure)
                return Result.Failure(result.Errors);

            await _reportRepository.UpdateAsync(report, cancellationToken);
            return Result.Success();
        }

        /// <summary>Update report KPIs.</summary>
        public async Task<Result> UpdateKPIsAsync(
            Guid reportId,
            KPICollection kpis,
            CancellationToken cancellationToken = default)
        {
            var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken);
            if (report == null)
                return Result.Failure(ReportingErrors.ReportNotFound);

            var result = report.UpdateKPIs(kpis);
            if (result.IsFailure)
                return Result.Failure(result.Errors);

            await _reportRepository.UpdateAsync(report, cancellationToken);
            return Result.Success();
        }

        /// <summary>Update report log details.</summary>
        public async Task<Result> UpdateLogDetailsAsync(
            Guid reportId,
            LogDetails logDetails,
            CancellationToken cancellationToken = default)
        {
            var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken);
            if (report == null)
                return Result.Failure(ReportingErrors.ReportNotFound);

            var result = report.UpdateLogDetails(logDetails);
            if (result.IsFailure)
                return Result.Failure(result.Errors);

            await _reportRepository.UpdateAsync(report, cancellationToken);
            return Result.Success();
        }
    }
}
