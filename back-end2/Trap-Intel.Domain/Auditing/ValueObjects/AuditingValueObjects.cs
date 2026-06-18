using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Auditing
{
    /// <summary>
    /// Value objects for the Auditing domain.
    /// </summary>

    /// <summary>
    /// Represents an IP address for audit logging.
    /// </summary>
    public record IpAddress
    {
        public string Value { get; }

        private IpAddress(string value) => Value = value;

        public static Result<IpAddress> Create(string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return Result.Failure<IpAddress>(AuditingErrors.InvalidIpAddress);

            var trimmed = ipAddress.Trim();

            // Basic IP validation (IPv4 and IPv6)
            if (!IsValidIpAddress(trimmed))
                return Result.Failure<IpAddress>(AuditingErrors.InvalidIpAddress);

            return Result.Success(new IpAddress(trimmed));
        }

        private static bool IsValidIpAddress(string ip)
        {
            return System.Net.IPAddress.TryParse(ip, out _);
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Represents changed values in an audit entry.
    /// </summary>
    public record AuditChange
    {
        public string PropertyName { get; }
        public string? OldValue { get; }
        public string? NewValue { get; }

        private AuditChange(string propertyName, string? oldValue, string? newValue)
        {
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public static Result<AuditChange> Create(string propertyName, string? oldValue, string? newValue)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return Result.Failure<AuditChange>(
                    Error.Custom("Auditing.InvalidPropertyName", "Property name cannot be empty."));

            return Result.Success(new AuditChange(propertyName.Trim(), oldValue, newValue));
        }
    }

    /// <summary>
    /// Represents a collection of audit changes.
    /// </summary>
    public record AuditChangeCollection
    {
        public IReadOnlyList<AuditChange> Changes { get; }

        private AuditChangeCollection(List<AuditChange> changes) => Changes = changes.AsReadOnly();

        public static Result<AuditChangeCollection> Create(List<AuditChange>? changes)
        {
            if (changes == null || changes.Count == 0)
                return Result.Success(new AuditChangeCollection(new List<AuditChange>()));

            return Result.Success(new AuditChangeCollection(changes));
        }

        public static AuditChangeCollection Empty() => new(new List<AuditChange>());
    }
}
