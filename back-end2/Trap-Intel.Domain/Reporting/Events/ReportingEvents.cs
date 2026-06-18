using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Reporting
{
    /// <summary>Raised when a report is generated.</summary>
    public class ReportGeneratedEvent : IDomainEvent
    {
        public Guid ReportId { get; }
        public Guid OrganizationId { get; }
        public ReportType ReportType { get; }
        public DateTime OccurredOn { get; }

        public ReportGeneratedEvent(Guid reportId, Guid organizationId,
            ReportType type, DateTime occurredAt)
        {
            ReportId = reportId;
            OrganizationId = organizationId;
            ReportType = type;
            OccurredOn = occurredAt;
        }
    }

    /// <summary>Raised when a report is reviewed.</summary>
    public class ReportReviewedEvent : IDomainEvent
    {
        public Guid ReportId { get; }
        public Guid ReviewerId { get; }
        public string ReviewNotes { get; }
        public DateTime OccurredOn { get; }

        public ReportReviewedEvent(Guid reportId, Guid reviewerId,
            string notes, DateTime occurredAt)
        {
            ReportId = reportId;
            ReviewerId = reviewerId;
            ReviewNotes = notes;
            OccurredOn = occurredAt;
        }
    }

    /// <summary>Raised when a report is sent.</summary>
    public class ReportSentEvent : IDomainEvent
    {
        public Guid ReportId { get; }
        public Guid RecipientId { get; }
        public string RecipientEmail { get; }
        public DateTime OccurredOn { get; }

        public ReportSentEvent(Guid reportId, Guid recipientId,
            string email, DateTime occurredAt)
        {
            ReportId = reportId;
            RecipientId = recipientId;
            RecipientEmail = email;
            OccurredOn = occurredAt;
        }
    }

    /// <summary>Raised when a report is updated.</summary>
    public class ReportUpdatedEvent : IDomainEvent
    {
        public Guid ReportId { get; }
        public string UpdateDescription { get; }
        public DateTime OccurredOn { get; }

        public ReportUpdatedEvent(Guid reportId, string description, DateTime occurredAt)
        {
            ReportId = reportId;
            UpdateDescription = description;
            OccurredOn = occurredAt;
        }
    }

    /// <summary>Raised when a template is created.</summary>
    public class TemplateCreatedEvent : IDomainEvent
    {
        public Guid TemplateId { get; }
        public ReportType ReportType { get; }
        public Guid CreatedBy { get; }
        public DateTime OccurredOn { get; }

        public TemplateCreatedEvent(Guid templateId, ReportType type,
            Guid createdBy, DateTime occurredAt)
        {
            TemplateId = templateId;
            ReportType = type;
            CreatedBy = createdBy;
            OccurredOn = occurredAt;
        }
    }

    /// <summary>Raised when a template section is added.</summary>
    public class TemplateSectionAddedEvent : IDomainEvent
    {
        public Guid TemplateId { get; }
        public string SectionName { get; }
        public DateTime OccurredOn { get; }

        public TemplateSectionAddedEvent(Guid templateId, string sectionName, DateTime occurredAt)
        {
            TemplateId = templateId;
            SectionName = sectionName;
            OccurredOn = occurredAt;
        }
    }

    /// <summary>Raised when an export is created.</summary>
    public class ExportCreatedEvent : IDomainEvent
    {
        public Guid ExportId { get; }
        public Guid ReportId { get; }
        public ReportFormat Format { get; }
        public DateTime OccurredOn { get; }

        public ExportCreatedEvent(Guid exportId, Guid reportId,
            ReportFormat format, DateTime occurredAt)
        {
            ExportId = exportId;
            ReportId = reportId;
            Format = format;
            OccurredOn = occurredAt;
        }
    }

    /// <summary>Raised when an export is completed.</summary>
    public class ExportCompletedEvent : IDomainEvent
    {
        public Guid ExportId { get; }
        public Guid ReportId { get; }
        public string FileUrl { get; }
        public DateTime OccurredOn { get; }

        public ExportCompletedEvent(Guid exportId, Guid reportId,
            string fileUrl, DateTime occurredAt)
        {
            ExportId = exportId;
            ReportId = reportId;
            FileUrl = fileUrl;
            OccurredOn = occurredAt;
        }
    }

    /// <summary>Raised when an export fails.</summary>
    public class ExportFailedEvent : IDomainEvent
    {
        public Guid ExportId { get; }
        public Guid ReportId { get; }
        public string ErrorMessage { get; }
        public DateTime OccurredOn { get; }

        public ExportFailedEvent(Guid exportId, Guid reportId,
            string error, DateTime occurredAt)
        {
            ExportId = exportId;
            ReportId = reportId;
            ErrorMessage = error;
            OccurredOn = occurredAt;
        }
    }
}
