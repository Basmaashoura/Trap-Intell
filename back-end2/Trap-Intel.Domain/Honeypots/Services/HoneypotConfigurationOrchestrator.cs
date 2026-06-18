using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Trap_Intel.Domain.Honeypots.Services
{
    public class HoneypotConfigurationOrchestrator
    {
        private readonly HoneypotPortValidator _portValidator;
        private readonly HoneypotTypeValidator _typeValidator;

        public HoneypotConfigurationOrchestrator(
            HoneypotPortValidator portValidator,
            HoneypotTypeValidator typeValidator)
        {
            _portValidator = portValidator ?? throw new ArgumentNullException(nameof(portValidator));
            _typeValidator = typeValidator ?? throw new ArgumentNullException(nameof(typeValidator));
        }

        public ConfigurationValidationResult ValidateConfiguration(
            HoneypotConfiguration config,
            HoneypotType type,
            List<Honeypot> existingHoneypots = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var errors = new List<string>();
            var warnings = new List<string>();

            var portValidation = _portValidator.ValidatePort(config.Port);
            if (!portValidation.IsValid)
            {
                errors.AddRange(portValidation.Errors);
            }
            warnings.AddRange(portValidation.Warnings);

            if (existingHoneypots != null)
            {
                var conflict = _portValidator.CheckPortConflict(config.Port, existingHoneypots);
                if (conflict.HasConflict)
                {
                    errors.Add(conflict.Message);
                }
            }

            var typeValidation = _typeValidator.ValidateTypeConfiguration(config, type);
            if (!typeValidation.IsValid)
            {
                errors.AddRange(typeValidation.Errors);
            }
            warnings.AddRange(typeValidation.Warnings);

            var captureLevelValidation = ValidateCaptureLevel(config.CaptureLevel, type);
            if (!captureLevelValidation.IsValid)
            {
                errors.AddRange(captureLevelValidation.Errors);
            }
            warnings.AddRange(captureLevelValidation.Warnings);

            var retentionValidation = ValidateRetentionDays(config.RetentionDays);
            if (!retentionValidation.IsValid)
            {
                errors.AddRange(retentionValidation.Errors);
            }
            warnings.AddRange(retentionValidation.Warnings);

            return new ConfigurationValidationResult(
                IsValid: errors.Count == 0,
                Errors: errors,
                Warnings: warnings);
        }

        public ValidationResult ValidateCaptureLevel(
            LogCaptureLevel captureLevel,
            HoneypotType type)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (captureLevel == LogCaptureLevel.Debug)
            {
                warnings.Add("Debug capture level may generate large amounts of data. " +
                           "Ensure adequate storage is available.");
            }

            if (captureLevel == LogCaptureLevel.Verbose)
            {
                if (type == HoneypotType.HTTP)
                {
                    warnings.Add("Verbose logging on HTTP honeypots may impact performance. " +
                               "Consider using Standard level for production.");
                }
            }

            return new ValidationResult(true, errors, warnings);
        }

        public ValidationResult ValidateRetentionDays(int retentionDays)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (retentionDays < 1)
            {
                errors.Add("Retention days must be at least 1.");
                return new ValidationResult(false, errors, warnings);
            }

            if (retentionDays > 365)
            {
                errors.Add("Retention days cannot exceed 365. For longer retention, use archival.");
                return new ValidationResult(false, errors, warnings);
            }

            if (retentionDays < 7)
            {
                warnings.Add("Retention period less than 7 days may result in data loss. " +
                           "Consider increasing to at least 30 days.");
            }

            if (retentionDays > 180)
            {
                warnings.Add($"Retention period of {retentionDays} days will consume significant storage. " +
                           $"Ensure adequate storage capacity is available.");
            }

            return new ValidationResult(true, errors, warnings);
        }

        public ValidationResult ValidateNetworkConfiguration(
            HoneypotNetworkInfo networkInfo)
        {
            if (networkInfo == null)
                throw new ArgumentNullException(nameof(networkInfo));

            var errors = new List<string>();
            var warnings = new List<string>();

            if (!IsValidIPAddress(networkInfo.IpAddress))
            {
                errors.Add($"Invalid IP address: {networkInfo.IpAddress}");
            }

            var portValidation = _portValidator.ValidatePort(networkInfo.Port);
            errors.AddRange(portValidation.Errors);
            warnings.AddRange(portValidation.Warnings);

            return new ValidationResult(errors.Count == 0, errors, warnings);
        }

        private bool IsValidIPAddress(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            var ipv4Pattern = @"^(\d{1,3}\.){3}\d{1,3}$";
            if (!Regex.IsMatch(ipAddress, ipv4Pattern))
                return false;

            var octets = ipAddress.Split('.');
            return octets.All(octet =>
            {
                if (!int.TryParse(octet, out var value))
                    return false;
                return value >= 0 && value <= 255;
            });
        }
    }
}
