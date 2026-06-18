using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Auditing
{
    /// <summary>
    /// Error definitions for the Auditing domain.
    /// </summary>
    public static class AuditingErrors
    {
        public static readonly Error AuditTrailNotFound = Error.Custom(
            "Auditing.AuditTrailNotFound",
            "The specified audit trail entry does not exist.");

        public static readonly Error InvalidAuditEntry = Error.Custom(
            "Auditing.InvalidAuditEntry",
            "The audit entry is invalid.");

        public static readonly Error InvalidResourceId = Error.Custom(
            "Auditing.InvalidResourceId",
            "The resource ID is invalid.");

        public static readonly Error InvalidResourceType = Error.Custom(
            "Auditing.InvalidResourceType",
            "The resource type is invalid.");

        public static readonly Error InvalidAuditAction = Error.Custom(
            "Auditing.InvalidAuditAction",
            "The audit action is invalid.");

        public static readonly Error AlreadyAcknowledged = Error.Custom(
            "Auditing.AlreadyAcknowledged",
            "This audit log has already been acknowledged.");

        public static readonly Error InvalidIpAddress = Error.Custom(
            "Auditing.InvalidIpAddress",
            "The IP address is invalid.");

        public static readonly Error TamperedAuditLog = Error.Custom(
            "Auditing.TamperedAuditLog",
            "The audit log has been illegally modified and failed the integrity check.");
    }
}
