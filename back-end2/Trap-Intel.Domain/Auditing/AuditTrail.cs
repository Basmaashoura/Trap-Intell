using System;
using System.Collections.Generic;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Auditing
{
    /// <summary>
    /// Represents an audit trail entry for compliance and security tracking.
    /// Append-only immutable log of all significant system actions.
    /// </summary>
    public class AuditTrail : AggregateRoot<Guid>
    {
        private List<AuditChange> _changes = new();
        private List<ComplianceStandard> _complianceStandards = new();

        private AuditTrail() { }

        private AuditTrail(
            Guid id,
            Guid organizationId,
            Guid? userId,
            AuditResourceType resourceType,
            Guid resourceId,
            AuditAction action,
            AuditSeverity severity)
            : base(id)
        {
            OrganizationId = organizationId;
            UserId = userId;
            ResourceType = resourceType;
            ResourceId = resourceId;
            Action = action;
            Severity = severity;
            Timestamp = DateTime.UtcNow;
            RetentionPeriodDays = 365; // Default: 1 year
        }

        // Properties
        public Guid OrganizationId { get; private set; }
        public Guid? UserId { get; private set; }
        public AuditResourceType ResourceType { get; private set; }
        public Guid ResourceId { get; private set; }
        public AuditAction Action { get; private set; }
        public AuditSeverity Severity { get; private set; }
        public string? Reason { get; private set; }
        public string? IpAddress { get; private set; }
        public string? UserAgent { get; private set; }
        public DateTime Timestamp { get; private set; }
        public int RetentionPeriodDays { get; private set; }
        public bool IsArchived { get; private set; }
        public bool IsAcknowledged { get; private set; }
        public Guid? AcknowledgedBy { get; private set; }
        public DateTime? AcknowledgedAt { get; private set; }
        public string? AcknowledgeNotes { get; private set; }

        /// <summary>
        /// Highly secure tamper-proof signature representing the initial valid state.
        /// </summary>
        public string? RecordHash { get; private set; }

        public IReadOnlyList<AuditChange> Changes => _changes.AsReadOnly();

        /// <summary>
        /// Gets the expiration date of this audit entry (when it can be deleted).
        /// </summary>
        public DateTime ExpirationDate => Timestamp.AddDays(RetentionPeriodDays);

        /// <summary>
        /// Check if this audit entry has expired.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow > ExpirationDate;

        #region Factory Methods

        /// <summary>
        /// Factory method to create a new audit trail entry.
        /// </summary>
        public static Result<AuditTrail> Create(
            Guid organizationId,
            Guid? userId,
            AuditResourceType resourceType,
            Guid resourceId,
            AuditAction action,
            AuditSeverity severity = AuditSeverity.Info,
            string? reason = null,
            string? ipAddress = null,
            string? userAgent = null,
            int retentionDays = 365)
        {
            // Validation
            if (organizationId == Guid.Empty)
                return Result.Failure<AuditTrail>(AuditingErrors.InvalidResourceId);

            if (resourceId == Guid.Empty)
                return Result.Failure<AuditTrail>(AuditingErrors.InvalidResourceId);

            if (retentionDays <= 0 || retentionDays > 2555) // Max 7 years
                return Result.Failure<AuditTrail>(
                    Error.Custom("Auditing.InvalidRetentionPeriod", "Retention period must be between 1 and 2555 days."));

            var auditTrail = new AuditTrail(
                Guid.NewGuid(),
                organizationId,
                userId,
                resourceType,
                resourceId,
                action,
                severity)
            {
                Reason = reason,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                RetentionPeriodDays = retentionDays
            };

            auditTrail.RaiseDomainEvent(new AuditRecordedEvent(
                auditTrail.Id,
                organizationId,
                userId,
                resourceType,
                resourceId,
                action,
                severity,
                DateTime.UtcNow));

            if (severity == AuditSeverity.Critical)
            {
                    auditTrail.RaiseDomainEvent(new CriticalAuditLogRecordedEvent(
                            auditTrail.Id,
                            organizationId,
                            resourceType,
                            resourceId,
                            action,
                            DateTime.UtcNow));
                    }

                    // Seal the audit record to guarantee immutability going into DB
                    auditTrail.SealAudit();

                    return Result.Success(auditTrail);
                }

        /// <summary>
        /// Factory method to reconstruct audit trail from database.
        /// </summary>
        public static AuditTrail Reconstruct(
            Guid id,
            Guid organizationId,
            Guid? userId,
            AuditResourceType resourceType,
            Guid resourceId,
            AuditAction action,
            AuditSeverity severity,
            string? reason,
            string? ipAddress,
            string? userAgent,
            DateTime timestamp,
            int retentionPeriodDays,
            bool isArchived,
            bool isAcknowledged,
            Guid? acknowledgedBy,
            DateTime? acknowledgedAt,
            string? acknowledgeNotes,
            string? recordHash = null,
            List<AuditChange>? changes = null)
        {
            var auditTrail = new AuditTrail(
                id,
                organizationId,
                userId,
                resourceType,
                resourceId,
                action,
                severity)
            {
                Reason = reason,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = timestamp,
                RetentionPeriodDays = retentionPeriodDays,
                IsArchived = isArchived,
                IsAcknowledged = isAcknowledged,
                AcknowledgedBy = acknowledgedBy,
                AcknowledgedAt = acknowledgedAt,
                AcknowledgeNotes = acknowledgeNotes,
                RecordHash = recordHash,
                _changes = changes ?? new()
            };

            return auditTrail;
        }

        #endregion

        #region Auditing Lifecycle

        /// <summary>
        /// Acknowledges a critical audit log by an administrator.
        /// </summary>
        public Result Acknowledge(Guid userId, string? notes = null)
        {
            if (IsAcknowledged)
                return Result.Failure(AuditingErrors.AlreadyAcknowledged);

            IsAcknowledged = true;
            AcknowledgedBy = userId;
            AcknowledgedAt = DateTime.UtcNow;
            AcknowledgeNotes = notes;

            RaiseDomainEvent(new AuditLogAcknowledgedEvent(Id, userId, DateTime.UtcNow, DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Archives the audit log entry, putting it into cold storage instead of hard deletion.
        /// </summary>
        public Result Archive()
        {
            if (IsArchived)
                return Result.Success();

            IsArchived = true;
            return Result.Success();
        }

        #endregion

        #region Domain Operations

        /// <summary>
        /// Computes SHA256 integrity hash of the original state to protect against tampering.
        /// </summary>
        private string ComputeHash()
        {
            var payload = $"{Id}|{OrganizationId}|{UserId}|{ResourceType}|{ResourceId}|{Action}|{Severity}|{Timestamp:O}|{Reason}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Secures the current Audit Trail instance for immutability checking.
        /// </summary>
        public void SealAudit()
        {
            if (string.IsNullOrEmpty(RecordHash))
            {
                RecordHash = ComputeHash();
            }
        }

        /// <summary>
        /// Verifies that the audit log properties have not been maliciously tampered with in DB.
        /// </summary>
        public Result VerifyIntegrity()
        {
            // If it never had a hash sealed, it is legacy or partial. We don't fail, but we can't guarantee.
            if (string.IsNullOrEmpty(RecordHash))
                return Result.Success();

            var currentHash = ComputeHash();
            if (currentHash != RecordHash)
                return Result.Failure(AuditingErrors.TamperedAuditLog);

            return Result.Success();
        }

        /// <summary>
        /// Add a change record to this audit entry.
        /// </summary>
        public Result AddChange(string propertyName, string? oldValue, string? newValue)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return Result.Failure(
                    Error.Custom("Auditing.InvalidPropertyName", "Property name cannot be empty."));

            var changeResult = AuditChange.Create(propertyName.Trim(), oldValue, newValue);
            if (changeResult.IsFailure)
                return Result.Failure(changeResult.Errors[0]);

            _changes.Add(changeResult.Value);

            return Result.Success();
        }

        /// <summary>
        /// Add multiple changes to this audit entry.
        /// </summary>
        public Result AddChanges(List<AuditChange> changes)
        {
            if (changes == null || changes.Count == 0)
                return Result.Success();

            _changes.AddRange(changes);
            return Result.Success();
        }

        #endregion

        #region Compliance Management

        /// <summary>
        /// Tag this audit entry for specific compliance standards.
        /// </summary>
        public Result AddComplianceStandard(ComplianceStandard standard)
        {
            if (_complianceStandards.Contains(standard))
                return Result.Success(); // Already tagged
                
            _complianceStandards.Add(standard);
            
            return Result.Success();
        }

        /// <summary>
        /// Get all compliance standards this audit entry satisfies.
        /// </summary>
        public IReadOnlyList<ComplianceStandard> ComplianceStandards => _complianceStandards.AsReadOnly();

        /// <summary>
        /// Check if audit entry is tagged for specific compliance standard.
        /// </summary>
        public bool IsTaggedForCompliance(ComplianceStandard standard)
        {
            return _complianceStandards.Contains(standard);
        }

        #endregion

        #region Query Helpers

        /// <summary>
        /// Check if audit is for specific resource type.
        /// </summary>
        public bool IsForResourceType(AuditResourceType resourceType)
        {
            return ResourceType == resourceType;
        }

        /// <summary>
        /// Check if audit was created by specific user.
        /// </summary>
        public bool IsCreatedByUser(Guid userId)
        {
            return UserId.HasValue && UserId.Value == userId;
        }

        /// <summary>
        /// Check if audit should be archived (past retention but not expired).
        /// </summary>
        public bool ShouldArchive(int archiveAfterDays = 90)
        {
            var archiveDate = Timestamp.AddDays(archiveAfterDays);
            return DateTime.UtcNow > archiveDate && !IsExpired;
        }

        /// <summary>
        /// Check if audit is high severity (warning or critical).
        /// </summary>
        public bool IsHighSeverity => Severity == AuditSeverity.Warning || Severity == AuditSeverity.Critical;

        #endregion
    }
}
