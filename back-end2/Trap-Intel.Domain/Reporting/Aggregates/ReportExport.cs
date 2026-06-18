using System;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Reporting
{
    /// <summary>
    /// Report export aggregate for managing export operations.
    /// Tracks export status and file locations.
    /// </summary>
    public class ReportExport : AggregateRoot<Guid>
    {
        private ReportExport() { }

        private ReportExport(
            Guid id,
            Guid reportId,
            Guid organizationId,
            Guid userId,
            ReportFormat format)
            : base(id)
        {
            ReportId = reportId;
            OrganizationId = organizationId;
            UserId = userId;
            Format = format;
            Status = ExportStatus.Pending;
            ExportDate = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
        }

        public Guid ReportId { get; }
        public Guid OrganizationId { get; }
        public Guid UserId { get; }
        public ReportFormat Format { get; }
        public ExportStatus Status { get; private set; }
        public DateTime ExportDate { get; }
        public string? FileUrl { get; private set; }
        public DateTime CreatedAt { get; }

        /// <summary>Factory method to create export.</summary>
        public static Result<ReportExport> Create(
            Guid reportId,
            Guid organizationId,
            Guid userId,
            ReportFormat format)
        {
            if (reportId == Guid.Empty)
                return Result.Failure<ReportExport>(ReportingErrors.InvalidReportId);

            if (organizationId == Guid.Empty)
                return Result.Failure<ReportExport>(ReportingErrors.InvalidOrganizationId);

            if (userId == Guid.Empty)
                return Result.Failure<ReportExport>(ReportingErrors.InvalidUserId);

            var export = new ReportExport(
                Guid.NewGuid(),
                reportId,
                organizationId,
                userId,
                format);

            export.RaiseDomainEvent(new ExportCreatedEvent(
                export.Id, reportId, format, DateTime.UtcNow));

            return Result.Success(export);
        }

        /// <summary>Mark export as completed.</summary>
        public Result MarkAsCompleted(string fileUrl)
        {
            if (Status == ExportStatus.Completed || Status == ExportStatus.Failed)
                return Result.Failure(ReportingErrors.CannotChangeExportStatus);

            if (string.IsNullOrWhiteSpace(fileUrl))
                return Result.Failure(ReportingErrors.InvalidFileUrl);

            Status = ExportStatus.Completed;
            FileUrl = fileUrl;

            RaiseDomainEvent(new ExportCompletedEvent(
                Id, ReportId, fileUrl, DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>Mark export as failed.</summary>
        public Result MarkAsFailed(string errorMessage)
        {
            if (Status == ExportStatus.Completed || Status == ExportStatus.Failed)
                return Result.Failure(ReportingErrors.CannotChangeExportStatus);

            if (string.IsNullOrWhiteSpace(errorMessage))
                return Result.Failure(ReportingErrors.InvalidErrorMessage);

            Status = ExportStatus.Failed;

            RaiseDomainEvent(new ExportFailedEvent(
                Id, ReportId, errorMessage, DateTime.UtcNow));

            return Result.Success();
        }
    }
}
