using System;
using System.Collections.Generic;
using System.Linq;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Auditing.Services
{
    /// <summary>
    /// Domain service for creating audit trail aggregates.
    /// This is a TRUE domain service - it encapsulates domain logic without infrastructure dependencies.
    /// 
    /// NOTE: Persistence should be handled by the Application Layer, not here.
    /// This service only creates AuditTrail aggregates following business rules.
    /// </summary>
    public class AuditService
    {
        /// <summary>
        /// Create an audit trail for a user action (most common).
        /// </summary>
        public Result<AuditTrail> CreateAuditLog(
            Guid organizationId,
            Guid userId,
            AuditResourceType resourceType,
            Guid resourceId,
            AuditAction action,
            string? reason = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            return AuditTrail.Create(
                organizationId,
                userId,
                resourceType,
                resourceId,
                action,
                AuditSeverity.Info,
                reason,
                ipAddress,
                userAgent);
        }

        /// <summary>
        /// Create an audit trail for a critical action (with severity level).
        /// </summary>
        public Result<AuditTrail> CreateCriticalAuditLog(
            Guid organizationId,
            Guid userId,
            AuditResourceType resourceType,
            Guid resourceId,
            AuditAction action,
            AuditSeverity severity = AuditSeverity.Warning,
            string? reason = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            return AuditTrail.Create(
                organizationId,
                userId,
                resourceType,
                resourceId,
                action,
                severity,
                reason,
                ipAddress,
                userAgent);
        }

        /// <summary>
        /// Create an audit trail with detailed change tracking.
        /// </summary>
        public Result<AuditTrail> CreateAuditLogWithChanges(
            Guid organizationId,
            Guid userId,
            AuditResourceType resourceType,
            Guid resourceId,
            AuditAction action,
            List<AuditChange> changes,
            string? reason = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            if (changes is null || changes.Count == 0)
            {
                return Result.Failure<AuditTrail>(
                    Error.Custom("Audit.NoChanges", "Changes list cannot be null or empty."));
            }

            var auditResult = AuditTrail.Create(
                organizationId,
                userId,
                resourceType,
                resourceId,
                action,
                AuditSeverity.Info,
                reason,
                ipAddress,
                userAgent);

            if (auditResult.IsSuccess)
            {
                var audit = auditResult.Value;
                audit.AddChanges(changes);
            }

            return auditResult;
        }

        /// <summary>
        /// Create an audit trail for a system action (userId is null - automated process).
        /// </summary>
        public Result<AuditTrail> CreateSystemAuditLog(
            Guid organizationId,
            AuditResourceType resourceType,
            Guid resourceId,
            AuditAction action,
            string? reason = null)
        {
            return AuditTrail.Create(
                organizationId,
                null, // System action
                resourceType,
                resourceId,
                action,
                AuditSeverity.Info,
                reason ?? "System automated action");
        }

        /// <summary>
        /// Create an audit trail for a failed operation (error severity).
        /// </summary>
        public Result<AuditTrail> CreateFailureAuditLog(
            Guid organizationId,
            Guid? userId,
            AuditResourceType resourceType,
            Guid resourceId,
            AuditAction action,
            string errorMessage,
            string? ipAddress = null,
            string? userAgent = null)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                return Result.Failure<AuditTrail>(
                    Error.Custom("Audit.InvalidErrorMessage", "Error message cannot be empty."));
            }

            return AuditTrail.Create(
                organizationId,
                userId,
                resourceType,
                resourceId,
                action,
                AuditSeverity.Error,
                errorMessage,
                ipAddress,
                userAgent);
        }

        /// <summary>
        /// Create multiple audit logs as a batch operation.
        /// Useful for tracking multiple related changes in a single transaction.
        /// </summary>
        public Result<List<AuditTrail>> CreateBatchAuditLogs(
            Guid organizationId,
            Guid userId,
            List<(AuditResourceType ResourceType, Guid ResourceId, AuditAction Action, string? Reason)> entries,
            string? ipAddress = null,
            string? userAgent = null)
        {
            if (entries is null || entries.Count == 0)
            {
                return Result.Failure<List<AuditTrail>>(
                    Error.Custom("Audit.EmptyBatch", "Batch entries cannot be null or empty."));
            }

            var auditLogs = new List<AuditTrail>();
            var errors = new List<Error>();

            foreach (var entry in entries)
            {
                var result = AuditTrail.Create(
                    organizationId,
                    userId,
                    entry.ResourceType,
                    entry.ResourceId,
                    entry.Action,
                    AuditSeverity.Info,
                    entry.Reason,
                    ipAddress,
                    userAgent);

                if (result.IsSuccess)
                {
                    auditLogs.Add(result.Value);
                }
                else
                {
                    errors.AddRange(result.Errors);
                }
            }

            if (errors.Count > 0)
            {
                return Result.Failure<List<AuditTrail>>(errors);
            }

            return Result.Success(auditLogs);
        }

        /// <summary>
        /// Validate if an action should be audited based on severity and type.
        /// Domain logic for audit filtering.
        /// </summary>
        public bool ShouldAudit(AuditAction action, AuditSeverity severity)
        {
            // Always audit critical actions
            if (severity >= AuditSeverity.Warning)
                return true;

            // Always audit these sensitive actions regardless of severity
            var criticalActions = new[]
            {
                AuditAction.Delete,
                AuditAction.Update,
                AuditAction.Approve,
                AuditAction.Reject,
                AuditAction.Cancel
            };

            return criticalActions.Contains(action);
        }

        /// <summary>
        /// Determine appropriate severity level based on action type.
        /// Domain logic for severity classification.
        /// </summary>
        public AuditSeverity DetermineSeverity(AuditAction action)
        {
            return action switch
            {
                AuditAction.Delete => AuditSeverity.Warning,
                AuditAction.Approve => AuditSeverity.Warning,
                AuditAction.Reject => AuditSeverity.Warning,
                AuditAction.Cancel => AuditSeverity.Warning,
                AuditAction.Update => AuditSeverity.Info,
                AuditAction.Create => AuditSeverity.Info,
                AuditAction.View => AuditSeverity.Info,
                _ => AuditSeverity.Info
            };
        }
    }
}
