using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Reporting
{
    /// <summary>
    /// Rule: Report can only be sent if reviewed.
    /// </summary>
    public class ReportSendingRule : IBusinessRule
    {
        private readonly Report _report;

        public Error Error => ReportingErrors.CannotSendUnreviewedReport;

        public ReportSendingRule(Report report) => _report = report;

        public bool IsSatisfied()
        {
            return _report.Status == ReportStatus.Reviewed;
        }
    }

    /// <summary>
    /// Rule: Template must have at least one section.
    /// </summary>
    public class TemplateValidationRule : IBusinessRule
    {
        private readonly ReportTemplate _template;

        public Error Error => ReportingErrors.TemplateHasNoSections;

        public TemplateValidationRule(ReportTemplate template) => _template = template;

        public bool IsSatisfied()
        {
            return _template.Sections.Any();
        }
    }

    /// <summary>
    /// Rule: Report content must be valid.
    /// </summary>
    public class ReportContentValidationRule : IBusinessRule
    {
        private readonly Report _report;

        public Error Error => ReportingErrors.InvalidReportTitle;

        public ReportContentValidationRule(Report report) => _report = report;

        public bool IsSatisfied()
        {
            return _report != null &&
                   _report.KPIs != null &&
                   _report.LogDetails != null &&
                   _report.Recommendations != null;
        }
    }

    /// <summary>
    /// Rule: Export can only be created for existing report.
    /// </summary>
    public class ExportCreationRule : IBusinessRule
    {
        private readonly Guid _reportId;

        public Error Error => ReportingErrors.InvalidReportId;

        public ExportCreationRule(Guid reportId) => _reportId = reportId;

        public bool IsSatisfied()
        {
            return _reportId != Guid.Empty;
        }
    }
}
