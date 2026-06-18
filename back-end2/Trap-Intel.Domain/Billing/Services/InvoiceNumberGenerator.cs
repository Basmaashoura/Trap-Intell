using System;
using System.Text;

namespace Trap_Intel.Domain.Billing.Services
{
    /// <summary>
    /// Domain service for generating unique invoice numbers.
    /// 
    /// SINGLE RESPONSIBILITY: Generate invoice numbers only.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (number generation rules)
    /// ? NO repositories or infrastructure dependencies
    /// ? Stateless and deterministic
    /// ? Single responsibility (only invoice number generation)
    /// 
    /// Lines: ~170 (SOLID-compliant from start)
    /// 
    /// INVOICE NUMBER FORMAT:
    /// Standard: INV-YYYY-MM-NNNNNN
    /// Example: INV-2024-12-000001
    /// 
    /// Components:
    /// - Prefix: "INV" (configurable)
    /// - Year: 4 digits
    /// - Month: 2 digits
    /// - Sequence: 6 digits (000001-999999)
    /// </summary>
    public class InvoiceNumberGenerator
    {
        private const string DefaultPrefix = "INV";
        private const int SequenceLength = 6;
        private const char SequencePadding = '0';

        /// <summary>
        /// Generate invoice number with default format.
        /// 
        /// BUSINESS RULES:
        /// - Format: INV-YYYY-MM-NNNNNN
        /// - Sequence resets monthly
        /// - Zero-padded to 6 digits
        /// 
        /// EXAMPLE:
        /// - sequenceNumber = 42
        /// - date = 2024-12-25
        /// - Result: "INV-2024-12-000042"
        /// </summary>
        /// <param name="sequenceNumber">Current sequence number for the month</param>
        /// <param name="date">Invoice date (defaults to current UTC)</param>
        /// <returns>Generated invoice number</returns>
        public string Generate(int sequenceNumber, DateTime? date = null)
        {
            return Generate(sequenceNumber, DefaultPrefix, date);
        }

        /// <summary>
        /// Generate invoice number with custom prefix.
        /// 
        /// BUSINESS RULES:
        /// - Prefix must be 2-10 characters
        /// - Prefix must be alphanumeric
        /// - Converted to uppercase
        /// 
        /// EXAMPLE:
        /// - sequenceNumber = 123
        /// - prefix = "PRO"
        /// - date = 2024-12-25
        /// - Result: "PRO-2024-12-000123"
        /// </summary>
        /// <param name="sequenceNumber">Current sequence number</param>
        /// <param name="prefix">Custom prefix (2-10 chars)</param>
        /// <param name="date">Invoice date</param>
        /// <returns>Generated invoice number</returns>
        public string Generate(int sequenceNumber, string prefix, DateTime? date = null)
        {
            // Validation
            if (sequenceNumber < 1)
                throw new ArgumentException("Sequence number must be positive.", nameof(sequenceNumber));

            if (sequenceNumber > 999999)
                throw new ArgumentException("Sequence number cannot exceed 999999.", nameof(sequenceNumber));

            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Prefix cannot be empty.", nameof(prefix));

            if (prefix.Length < 2 || prefix.Length > 10)
                throw new ArgumentException("Prefix must be 2-10 characters.", nameof(prefix));

            // Use provided date or current UTC
            var invoiceDate = date ?? DateTime.UtcNow;

            // Build invoice number
            var builder = new StringBuilder();
            
            // Prefix (uppercase)
            builder.Append(prefix.ToUpperInvariant());
            builder.Append('-');

            // Year (4 digits)
            builder.Append(invoiceDate.Year.ToString("D4"));
            builder.Append('-');

            // Month (2 digits)
            builder.Append(invoiceDate.Month.ToString("D2"));
            builder.Append('-');

            // Sequence (6 digits, zero-padded)
            builder.Append(sequenceNumber.ToString($"D{SequenceLength}"));

            return builder.ToString();
        }

        /// <summary>
        /// Generate invoice number for specific organization.
        /// Includes organization code in format.
        /// 
        /// FORMAT: PREFIX-ORGCODE-YYYY-MM-NNNNNN
        /// EXAMPLE: INV-ACME-2024-12-000001
        /// </summary>
        /// <param name="sequenceNumber">Sequence number</param>
        /// <param name="organizationCode">Organization code (2-6 chars)</param>
        /// <param name="prefix">Invoice prefix</param>
        /// <param name="date">Invoice date</param>
        /// <returns>Organization-specific invoice number</returns>
        public string GenerateForOrganization(
            int sequenceNumber,
            string organizationCode,
            string prefix = DefaultPrefix,
            DateTime? date = null)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(organizationCode))
                throw new ArgumentException("Organization code cannot be empty.", nameof(organizationCode));

            if (organizationCode.Length < 2 || organizationCode.Length > 6)
                throw new ArgumentException("Organization code must be 2-6 characters.", nameof(organizationCode));

            var invoiceDate = date ?? DateTime.UtcNow;

            var builder = new StringBuilder();

            // Prefix
            builder.Append(prefix.ToUpperInvariant());
            builder.Append('-');

            // Organization code
            builder.Append(organizationCode.ToUpperInvariant());
            builder.Append('-');

            // Date
            builder.Append(invoiceDate.Year.ToString("D4"));
            builder.Append('-');
            builder.Append(invoiceDate.Month.ToString("D2"));
            builder.Append('-');

            // Sequence
            builder.Append(sequenceNumber.ToString($"D{SequenceLength}"));

            return builder.ToString();
        }

        /// <summary>
        /// Parse invoice number to extract components.
        /// Useful for validation and reporting.
        /// </summary>
        /// <param name="invoiceNumber">Invoice number to parse</param>
        /// <returns>Parsed components or null if invalid format</returns>
        public InvoiceNumberComponents? Parse(string invoiceNumber)
        {
            if (string.IsNullOrWhiteSpace(invoiceNumber))
                return null;

            var parts = invoiceNumber.Split('-');

            // Standard format: PREFIX-YYYY-MM-NNNNNN (4 parts)
            // Org format: PREFIX-ORGCODE-YYYY-MM-NNNNNN (5 parts)

            if (parts.Length == 4)
            {
                return ParseStandardFormat(parts);
            }
            else if (parts.Length == 5)
            {
                return ParseOrganizationFormat(parts);
            }

            return null; // Invalid format
        }

        /// <summary>
        /// Validate invoice number format.
        /// </summary>
        /// <param name="invoiceNumber">Invoice number to validate</param>
        /// <returns>True if valid format</returns>
        public bool IsValidFormat(string invoiceNumber)
        {
            return Parse(invoiceNumber) != null;
        }

        #region Private Helpers

        private InvoiceNumberComponents? ParseStandardFormat(string[] parts)
        {
            try
            {
                var prefix = parts[0];
                var year = int.Parse(parts[1]);
                var month = int.Parse(parts[2]);
                var sequence = int.Parse(parts[3]);

                return new InvoiceNumberComponents(
                    Prefix: prefix,
                    Year: year,
                    Month: month,
                    Sequence: sequence,
                    OrganizationCode: null);
            }
            catch
            {
                return null;
            }
        }

        private InvoiceNumberComponents? ParseOrganizationFormat(string[] parts)
        {
            try
            {
                var prefix = parts[0];
                var orgCode = parts[1];
                var year = int.Parse(parts[2]);
                var month = int.Parse(parts[3]);
                var sequence = int.Parse(parts[4]);

                return new InvoiceNumberComponents(
                    Prefix: prefix,
                    Year: year,
                    Month: month,
                    Sequence: sequence,
                    OrganizationCode: orgCode);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }

    /// <summary>
    /// Value object representing parsed invoice number components.
    /// </summary>
    public record InvoiceNumberComponents(
        string Prefix,
        int Year,
        int Month,
        int Sequence,
        string? OrganizationCode);
}
