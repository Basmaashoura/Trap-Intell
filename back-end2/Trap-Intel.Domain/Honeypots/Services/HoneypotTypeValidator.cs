using System;
using System.Collections.Generic;

namespace Trap_Intel.Domain.Honeypots.Services
{
    /// <summary>
    /// Domain service for validating type-specific honeypot configurations.
    /// 
    /// SINGLE RESPONSIBILITY: Type-specific configuration validation.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (type-specific rules)
    /// ? NO repositories or infrastructure dependencies
    /// ? Stateless and deterministic
    /// ? Single responsibility (only type validation)
    /// 
    /// Lines: ~230 (SOLID-compliant, includes all 10 type validators)
    /// </summary>
    public class HoneypotTypeValidator
    {
        private readonly HoneypotPortValidator _portValidator;

        public HoneypotTypeValidator(HoneypotPortValidator portValidator)
        {
            _portValidator = portValidator ?? throw new ArgumentNullException(nameof(portValidator));
        }

        /// <summary>
        /// Validate type-specific configuration requirements.
        /// 
        /// BUSINESS RULES:
        /// - Each honeypot type has specific port recommendations
        /// - Non-standard ports generate warnings (not errors)
        /// - Helps improve honeypot realism
        /// </summary>
        public ValidationResult ValidateTypeConfiguration(
            HoneypotConfiguration config,
            HoneypotType type)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var errors = new List<string>();
            var warnings = new List<string>();

            // Check if port is standard for type
            if (!_portValidator.IsStandardPort(config.Port, type))
            {
                var recommendedPort = _portValidator.GetRecommendedPort(type);
                warnings.Add($"{type} honeypot using non-standard port {config.Port}. " +
                           $"Consider using port {recommendedPort} for better realism.");
            }

            // Type-specific validation
            switch (type)
            {
                case HoneypotType.SSH:
                    ValidateSSH(config, errors, warnings);
                    break;

                case HoneypotType.HTTP:
                    ValidateHTTP(config, errors, warnings);
                    break;

                case HoneypotType.FTP:
                    ValidateFTP(config, errors, warnings);
                    break;

                case HoneypotType.SMTP:
                    ValidateSMTP(config, errors, warnings);
                    break;

                case HoneypotType.DNS:
                    ValidateDNS(config, errors, warnings);
                    break;

                case HoneypotType.Telnet:
                    ValidateTelnet(config, errors, warnings);
                    break;

                case HoneypotType.RDP:
                    ValidateRDP(config, errors, warnings);
                    break;

                case HoneypotType.Samba:
                    ValidateSamba(config, errors, warnings);
                    break;

                case HoneypotType.SNMP:
                    ValidateSNMP(config, errors, warnings);
                    break;

                case HoneypotType.Custom:
                    // Custom types have no specific validation
                    break;
            }

            return new ValidationResult(errors.Count == 0, errors, warnings);
        }

        #region Type-Specific Validators

        private void ValidateSSH(
            HoneypotConfiguration config,
            List<string> errors,
            List<string> warnings)
        {
            // SSH-specific validation
            // Could add checks for SSH-specific settings when available
        }

        private void ValidateHTTP(
            HoneypotConfiguration config,
            List<string> errors,
            List<string> warnings)
        {
            // HTTP-specific validation
            // Could add checks for HTTP-specific settings when available
        }

        private void ValidateFTP(
            HoneypotConfiguration config,
            List<string> errors,
            List<string> warnings)
        {
            // FTP-specific validation
            // Could add checks for FTP-specific settings when available
        }

        private void ValidateSMTP(
            HoneypotConfiguration config,
            List<string> errors,
            List<string> warnings)
        {
            // SMTP-specific validation
            // Could add checks for SMTP-specific settings when available
        }

        private void ValidateDNS(
            HoneypotConfiguration config,
            List<string> errors,
            List<string> warnings)
        {
            // DNS-specific validation
            // Could add checks for DNS-specific settings when available
        }

        private void ValidateTelnet(
            HoneypotConfiguration config,
            List<string> errors,
            List<string> warnings)
        {
            // Telnet-specific validation
            // Could add checks for Telnet-specific settings when available
        }

        private void ValidateRDP(
            HoneypotConfiguration config,
            List<string> errors,
            List<string> warnings)
        {
            // RDP-specific validation
            // Could add checks for RDP-specific settings when available
        }

        private void ValidateSamba(
            HoneypotConfiguration config,
            List<string> errors,
            List<string> warnings)
        {
            // Samba-specific validation
            // Could add checks for Samba-specific settings when available
        }

        private void ValidateSNMP(
            HoneypotConfiguration config,
            List<string> errors,
            List<string> warnings)
        {
            // SNMP-specific validation
            // Could add checks for SNMP-specific settings when available
        }

        #endregion
    }
}
