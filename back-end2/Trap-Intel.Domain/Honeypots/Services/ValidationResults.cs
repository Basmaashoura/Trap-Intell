using System;
using System.Collections.Generic;

namespace Trap_Intel.Domain.Honeypots.Services
{
    /// <summary>
    /// Validation result value objects for honeypot configuration validation.
    /// Used by HoneypotConfigurationOrchestrator and validators.
    /// </summary>

    /// <summary>
    /// Result of configuration validation.
    /// Contains errors (blocking) and warnings (advisory).
    /// </summary>
    public record ConfigurationValidationResult(
        bool IsValid,
        List<string> Errors,
        List<string> Warnings)
    {
        public bool HasWarnings => Warnings.Count > 0;
        
        public string GetSummary() =>
            IsValid
                ? $"Configuration valid. {Warnings.Count} warning(s)."
                : $"Configuration invalid. {Errors.Count} error(s), {Warnings.Count} warning(s).";
    }

    /// <summary>
    /// Generic validation result.
    /// Used for individual validation steps (port, type, capture level, etc.).
    /// </summary>
    public record ValidationResult(
        bool IsValid,
        List<string> Errors,
        List<string> Warnings);

    /// <summary>
    /// Result of port conflict check.
    /// Identifies which honeypot is using the conflicting port.
    /// </summary>
    public record PortConflictResult(
        bool HasConflict,
        string? Message,
        Guid? ConflictingHoneypotId = null);
}
