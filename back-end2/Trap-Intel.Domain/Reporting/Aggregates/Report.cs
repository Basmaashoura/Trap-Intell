using System;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Reporting
{
    /// <summary>
    /// Report aggregate root for AI log analysis reports.
    /// Manages report lifecycle, content, and status transitions.
    /// </summary>
    public class Report : AggregateRoot<Guid>
    {
        private Report() { }

        private Report(
            Guid id,
            Guid organizationId,
            ReportType type,
            ReportTitle title,
            ReportSummary summary,
            KPICollection kpis,
            LogDetails logDetails,
            RecommendationCollection recommendations)
            : base(id)
        {
            OrganizationId = organizationId;
            Type = type;
            Title = title;
            Summary = summary;
            KPIs = kpis;
            LogDetails = logDetails;
            Recommendations = recommendations;
            Status = ReportStatus.Draft;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid OrganizationId { get; }
        public Guid? UserId { get; private set; }
        public Guid? SubscriptionId { get; private set; }
        public ReportType Type { get; private set; }
        public ReportTitle Title { get; private set; }
        public ReportSummary Summary { get; private set; }
        public KPICollection KPIs { get; private set; }
        public LogDetails LogDetails { get; private set; }
        public RecommendationCollection Recommendations { get; private set; }
        public ReportStatus Status { get; private set; }
        public ReportFormat Format { get; private set; }
        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; private set; }

        /// <summary>Factory method to create a new report.</summary>
        public static Result<Report> Create(
            Guid organizationId,
            ReportType type,
            ReportTitle title,
            ReportSummary summary,
            KPICollection kpis,
            LogDetails logDetails,
            RecommendationCollection recommendations,
            Guid? userId = null,
            Guid? subscriptionId = null)
        {
            if (organizationId == Guid.Empty)
                return Result.Failure<Report>(ReportingErrors.InvalidOrganizationId);

            if (kpis == null)
                return Result.Failure<Report>(ReportingErrors.InvalidKPIs);

            if (logDetails == null)
                return Result.Failure<Report>(ReportingErrors.InvalidLogDetails);

            if (recommendations == null)
                return Result.Failure<Report>(ReportingErrors.InvalidRecommendationCollection);

            var report = new Report(
                Guid.NewGuid(),
                organizationId,
                type,
                title,
                summary,
                kpis,
                logDetails,
                recommendations)
            {
                UserId = userId,
                SubscriptionId = subscriptionId
            };

            report.RaiseDomainEvent(new ReportGeneratedEvent(
                report.Id, organizationId, type, DateTime.UtcNow));

            return Result.Success(report);
        }

        /// <summary>Review and approve report.</summary>
        public Result Review(Guid reviewerId, string reviewNotes)
        {
            if (reviewerId == Guid.Empty)
                return Result.Failure(ReportingErrors.InvalidReviewerId);

            if (Status != ReportStatus.Draft && Status != ReportStatus.Generated)
                return Result.Failure(ReportingErrors.CannotReviewReport);

            Status = ReportStatus.Reviewed;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new ReportReviewedEvent(
                Id, reviewerId, reviewNotes, DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>Send report to recipient.</summary>
        public Result Send(Guid recipientId, string recipientEmail)
        {
            var rule = new ReportSendingRule(this);
            if (!rule.IsSatisfied())
                return Result.Failure(rule.Error);

            if (string.IsNullOrWhiteSpace(recipientEmail))
                return Result.Failure(ReportingErrors.InvalidRecipientEmail);

            Status = ReportStatus.Sent;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new ReportSentEvent(
                Id, recipientId, recipientEmail, DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>Set export format.</summary>
        public Result SetFormat(ReportFormat format)
        {
            Format = format;
            UpdatedAt = DateTime.UtcNow;
            return Result.Success();
        }

        /// <summary>Update KPIs.</summary>
        public Result UpdateKPIs(KPICollection kpis)
        {
            if (kpis == null)
                return Result.Failure(ReportingErrors.InvalidKPIs);

            KPIs = kpis;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new ReportUpdatedEvent(
                Id, "KPIs updated", DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>Update log details.</summary>
        public Result UpdateLogDetails(LogDetails logDetails)
        {
            if (logDetails == null)
                return Result.Failure(ReportingErrors.InvalidLogDetails);

            LogDetails = logDetails;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new ReportUpdatedEvent(
                Id, "Log details updated", DateTime.UtcNow));

            return Result.Success();
        }
    }
}
