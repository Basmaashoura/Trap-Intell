using System;
using System.Collections.Generic;
using System.Linq;

namespace Trap_Intel.Domain.Honeypots.Services
{
    /// <summary>
    /// Domain service for validating honeypot port configurations.
    /// 
    /// SINGLE RESPONSIBILITY: Port validation and conflict detection.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (port validation)
    /// ? NO repositories or infrastructure dependencies
    /// ? Stateless and deterministic
    /// ? Single responsibility (only port validation)
    /// 
    /// Lines: ~150 (SOLID-compliant)
    /// </summary>
    public class HoneypotPortValidator
    {
        // Standard port ranges
        private const int MinPort = 1;
        private const int MaxPort = 65535;
        private const int WellKnownPortsMax = 1023;
        private const int RegisteredPortsMax = 49151;

        // Common reserved ports that shouldn't be used for honeypots
        private static readonly HashSet<int> ReservedPorts = new()
        {
            20, 21,    // FTP
            25,        // SMTP
            53,        // DNS
            110,       // POP3
            143,       // IMAP
            443,       // HTTPS (system)
            3306,      // MySQL
            5432,      // PostgreSQL
            27017      // MongoDB
        };

        /// <summary>
        /// Validate port number.
        /// 
        /// BUSINESS RULES:
        /// - Port must be between 1-65535
        /// - Warn if using well-known ports (1-1023)
        /// - Warn if using reserved system ports
        /// </summary>
        public ValidationResult ValidatePort(int port)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Rule 1: Port must be in valid range
            if (port < MinPort || port > MaxPort)
            {
                errors.Add($"Port {port} is invalid. Must be between {MinPort} and {MaxPort}.");
                return new ValidationResult(false, errors, warnings);
            }

            // Rule 2: Check if port is reserved
            if (ReservedPorts.Contains(port))
            {
                warnings.Add($"Port {port} is commonly used by system services. " +
                           $"This may cause conflicts. Consider using a high port (49152-65535).");
            }

            // Rule 3: Warn about well-known ports
            if (port <= WellKnownPortsMax)
            {
                warnings.Add($"Port {port} is in the well-known ports range (1-1023). " +
                           $"May require elevated privileges.");
            }

            return new ValidationResult(true, errors, warnings);
        }

        /// <summary>
        /// Check if port conflicts with existing honeypots.
        /// 
        /// BUSINESS RULE:
        /// - Each honeypot must use a unique port
        /// - Only check active, paused, and provisioning honeypots
        /// </summary>
        public PortConflictResult CheckPortConflict(
            int port,
            List<Honeypot> existingHoneypots)
        {
            if (existingHoneypots == null)
                return new PortConflictResult(false, null);

            // Only check non-terminated honeypots
            var activeHoneypots = existingHoneypots.Where(h =>
                h.Status != HoneypotStatus.Terminated &&
                h.Status != HoneypotStatus.Retired).ToList();

            var conflictingHoneypot = activeHoneypots.FirstOrDefault(h =>
                h.Configuration.Port == port);

            if (conflictingHoneypot != null)
            {
                return new PortConflictResult(
                    HasConflict: true,
                    Message: $"Port {port} is already in use by honeypot '{conflictingHoneypot.Name}' " +
                            $"(Status: {conflictingHoneypot.Status}). Choose a different port.",
                    ConflictingHoneypotId: conflictingHoneypot.Id);
            }

            return new PortConflictResult(false, null);
        }

        /// <summary>
        /// Get recommended port for a honeypot type.
        /// Helps users select appropriate ports.
        /// </summary>
        public int GetRecommendedPort(HoneypotType type)
        {
            return type switch
            {
                HoneypotType.SSH => 22,
                HoneypotType.HTTP => 80,
                HoneypotType.FTP => 21,
                HoneypotType.SMTP => 25,
                HoneypotType.DNS => 53,
                HoneypotType.Telnet => 23,
                HoneypotType.RDP => 3389,
                HoneypotType.Samba => 445,
                HoneypotType.SNMP => 161,
                _ => 8080 // Default for custom types
            };
        }

        /// <summary>
        /// Check if port is standard for the honeypot type.
        /// Used for validation warnings.
        /// </summary>
        public bool IsStandardPort(int port, HoneypotType type)
        {
            return type switch
            {
                HoneypotType.SSH => port == 22 || port == 2222 || (port >= 2200 && port <= 2299),
                HoneypotType.HTTP => port == 80 || port == 8080 || (port >= 8000 && port <= 8999),
                HoneypotType.FTP => port == 21 || port == 2121,
                HoneypotType.SMTP => port == 25 || port == 587 || port == 2525,
                HoneypotType.DNS => port == 53,
                HoneypotType.Telnet => port == 23,
                HoneypotType.RDP => port == 3389,
                HoneypotType.Samba => port == 445 || port == 139,
                HoneypotType.SNMP => port == 161,
                _ => true // Custom types accept any port
            };
        }
    }
}
