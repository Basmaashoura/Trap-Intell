using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Reporting
{
    /// <summary>
    /// Error codes for the Reporting & Analytics domain.
    /// </summary>
    public static class ReportingErrors
    {
        // Validation errors
        public static readonly Error InvalidOrganizationId = Error.Custom(
            "Reporting.InvalidOrganizationId",
            "Organization ID cannot be empty.");

        public static readonly Error InvalidUserId = Error.Custom(
            "Reporting.InvalidUserId",
            "User ID cannot be empty.");

        public static readonly Error InvalidReportId = Error.Custom(
            "Reporting.InvalidReportId",
            "Report ID cannot be empty.");

        public static readonly Error InvalidCreatedBy = Error.Custom(
            "Reporting.InvalidCreatedBy",
            "Created by user ID cannot be empty.");

        public static readonly Error InvalidReviewerId = Error.Custom(
            "Reporting.InvalidReviewerId",
            "Reviewer ID cannot be empty.");

        public static readonly Error InvalidRecipientEmail = Error.Custom(
            "Reporting.InvalidRecipientEmail",
            "Recipient email cannot be empty.");

        public static readonly Error InvalidErrorMessage = Error.Custom(
            "Reporting.InvalidErrorMessage",
            "Error message cannot be empty.");

        // Report title errors
        public static readonly Error InvalidReportTitle = Error.Custom(
            "Reporting.InvalidReportTitle",
            "Report title cannot be empty.");

        public static readonly Error ReportTitleTooShort = Error.Custom(
            "Reporting.ReportTitleTooShort",
            "Report title must be at least 5 characters.");

        public static readonly Error ReportTitleTooLong = Error.Custom(
            "Reporting.ReportTitleTooLong",
            "Report title cannot exceed 200 characters.");

        // Report summary errors
        public static readonly Error InvalidReportSummary = Error.Custom(
            "Reporting.InvalidReportSummary",
            "Report summary cannot be empty.");

        public static readonly Error ReportSummaryTooShort = Error.Custom(
            "Reporting.ReportSummaryTooShort",
            "Report summary must be at least 10 characters.");

        public static readonly Error ReportSummaryTooLong = Error.Custom(
            "Reporting.ReportSummaryTooLong",
            "Report summary cannot exceed 2000 characters.");

        // KPI errors
        public static readonly Error InvalidKPIs = Error.Custom(
            "Reporting.InvalidKPIs",
            "KPI collection cannot be null.");

        public static readonly Error InvalidKPIName = Error.Custom(
            "Reporting.InvalidKPIName",
            "KPI name cannot be empty.");

        public static readonly Error InvalidKPIUnit = Error.Custom(
            "Reporting.InvalidKPIUnit",
            "KPI unit cannot be empty.");

        public static readonly Error InvalidKPIThreshold = Error.Custom(
            "Reporting.InvalidKPIThreshold",
            "KPI threshold cannot be negative.");

        public static readonly Error EmptyKPICollection = Error.Custom(
            "Reporting.EmptyKPICollection",
            "KPI collection must contain at least one KPI.");

        // Log details errors
        public static readonly Error InvalidLogDetails = Error.Custom(
            "Reporting.InvalidLogDetails",
            "Log details cannot be null.");

        public static readonly Error InvalidLogCount = Error.Custom(
            "Reporting.InvalidLogCount",
            "Total log count cannot be negative.");

        public static readonly Error InvalidEventCount = Error.Custom(
            "Reporting.InvalidEventCount",
            "Event counts cannot be negative.");

        public static readonly Error LogCountMismatch = Error.Custom(
            "Reporting.LogCountMismatch",
            "Sum of events cannot exceed total logs.");

        public static readonly Error InvalidDateRange = Error.Custom(
            "Reporting.InvalidDateRange",
            "Start date must be before end date.");

        // Recommendation errors
        public static readonly Error InvalidRecommendationTitle = Error.Custom(
            "Reporting.InvalidRecommendationTitle",
            "Recommendation title cannot be empty.");

        public static readonly Error InvalidRecommendationDescription = Error.Custom(
            "Reporting.InvalidRecommendationDescription",
            "Recommendation description cannot be empty.");

        public static readonly Error InvalidImplementationDate = Error.Custom(
            "Reporting.InvalidImplementationDate",
            "Implementation date must be in the future.");

        public static readonly Error InvalidRecommendationCollection = Error.Custom(
            "Reporting.InvalidRecommendationCollection",
            "Recommendation collection cannot be null.");

        // Report action errors
        public static readonly Error CannotReviewReport = Error.Custom(
            "Reporting.CannotReviewReport",
            "Report cannot be reviewed in current status.");

        public static readonly Error CannotSendUnreviewedReport = Error.Custom(
            "Reporting.CannotSendUnreviewedReport",
            "Only reviewed reports can be sent.");

        public static readonly Error ReportNotFound = Error.Custom(
            "Reporting.ReportNotFound",
            "Report not found.");

        // Template errors
        public static readonly Error InvalidTemplateName = Error.Custom(
            "Reporting.InvalidTemplateName",
            "Template name cannot be empty.");

        public static readonly Error InvalidGuidelines = Error.Custom(
            "Reporting.InvalidGuidelines",
            "Template guidelines cannot be empty.");

        public static readonly Error InvalidTemplateSection = Error.Custom(
            "Reporting.InvalidTemplateSection",
            "Template section cannot be null.");

        public static readonly Error InvalidSectionName = Error.Custom(
            "Reporting.InvalidSectionName",
            "Section name cannot be empty.");

        public static readonly Error InvalidSectionDescription = Error.Custom(
            "Reporting.InvalidSectionDescription",
            "Section description cannot be empty.");

        public static readonly Error InvalidSectionOrder = Error.Custom(
            "Reporting.InvalidSectionOrder",
            "Section order cannot be negative.");

        public static readonly Error DuplicateTemplateSectionName = Error.Custom(
            "Reporting.DuplicateTemplateSectionName",
            "Template already contains a section with this name.");

        public static readonly Error SectionNotFound = Error.Custom(
            "Reporting.SectionNotFound",
            "Section not found in template.");

        public static readonly Error TemplateHasNoSections = Error.Custom(
            "Reporting.TemplateHasNoSections",
            "Template must have at least one section.");

        public static readonly Error TemplateNotFound = Error.Custom(
            "Reporting.TemplateNotFound",
            "Required template not found.");

        // Export errors
        public static readonly Error InvalidFileUrl = Error.Custom(
            "Reporting.InvalidFileUrl",
            "File URL cannot be empty.");

        public static readonly Error CannotChangeExportStatus = Error.Custom(
            "Reporting.CannotChangeExportStatus",
            "Cannot change status of completed or failed export.");

        public static readonly Error ExportNotFound = Error.Custom(
            "Reporting.ExportNotFound",
            "Export not found.");
    }
}
