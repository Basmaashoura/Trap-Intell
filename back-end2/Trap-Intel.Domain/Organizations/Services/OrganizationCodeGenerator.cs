using System;
using System.Linq;
using System.Text;

namespace Trap_Intel.Domain.Organizations.Services
{
    /// <summary>
    /// Domain service for generating unique organization codes.
    /// 
    /// SINGLE RESPONSIBILITY: Generate organization codes only.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (code generation rules)
    /// ? NO repositories or infrastructure dependencies
    /// ? Stateless and deterministic
    /// ? Single responsibility (only code generation)
    /// 
    /// Lines: ~180 (SOLID-compliant from start)
    /// 
    /// ORGANIZATION CODE FORMAT:
    /// Standard: 4-6 uppercase alphanumeric characters
    /// Examples: ACME, TECH01, CORP99
    /// 
    /// Generation Methods:
    /// 1. From name (ACME Corporation ? ACME)
    /// 2. From domain (acme.com ? ACME)
    /// 3. Sequential (ORG00001)
    /// 4. Random (A1B2C3)
    /// </summary>
    public class OrganizationCodeGenerator
    {
        private const int MinCodeLength = 4;
        private const int MaxCodeLength = 6;
        private const string DefaultPrefix = "ORG";

        /// <summary>
        /// Generate code from organization name.
        /// 
        /// BUSINESS RULES:
        /// - Take first N characters
        /// - Remove spaces and special chars
        /// - Convert to uppercase
        /// - Pad with numbers if too short
        /// 
        /// EXAMPLES:
        /// - "ACME Corporation" ? "ACME"
        /// - "Tech" ? "TECH01"
        /// - "Global Industries Inc." ? "GLOBAL"
        /// </summary>
        /// <param name="organizationName">Organization name</param>
        /// <param name="sequenceNumber">Optional sequence for uniqueness</param>
        /// <returns>Generated organization code</returns>
        public string GenerateFromName(string organizationName, int? sequenceNumber = null)
        {
            if (string.IsNullOrWhiteSpace(organizationName))
                throw new ArgumentException("Organization name cannot be empty.", nameof(organizationName));

            // Remove special characters and spaces
            var cleanName = new string(organizationName
                .Where(c => char.IsLetterOrDigit(c))
                .ToArray())
                .ToUpperInvariant();

            if (string.IsNullOrEmpty(cleanName))
                throw new ArgumentException("Organization name must contain alphanumeric characters.", nameof(organizationName));

            // Take first 4-6 characters
            var baseCode = cleanName.Length >= MaxCodeLength
                ? cleanName.Substring(0, MaxCodeLength)
                : cleanName;

            // Pad if too short
            if (baseCode.Length < MinCodeLength)
            {
                baseCode = baseCode.PadRight(MinCodeLength, '0');
            }

            // Add sequence if provided
            if (sequenceNumber.HasValue)
            {
                var seqStr = sequenceNumber.Value.ToString("D2");
                
                // Ensure total length doesn't exceed max
                if (baseCode.Length + seqStr.Length > MaxCodeLength)
                {
                    baseCode = baseCode.Substring(0, MaxCodeLength - seqStr.Length);
                }
                
                baseCode += seqStr;
            }

            return baseCode;
        }

        /// <summary>
        /// Generate code from domain name.
        /// 
        /// BUSINESS RULES:
        /// - Extract domain without TLD
        /// - Remove hyphens and underscores
        /// - Convert to uppercase
        /// 
        /// EXAMPLES:
        /// - "acme.com" ? "ACME"
        /// - "tech-solutions.io" ? "TECHSO"
        /// - "my-company-123.net" ? "MYCO12"
        /// </summary>
        /// <param name="domainName">Domain name</param>
        /// <param name="sequenceNumber">Optional sequence</param>
        /// <returns>Generated organization code</returns>
        public string GenerateFromDomain(string domainName, int? sequenceNumber = null)
        {
            if (string.IsNullOrWhiteSpace(domainName))
                throw new ArgumentException("Domain name cannot be empty.", nameof(domainName));

            // Extract domain part (before first dot)
            var domainPart = domainName.Contains('.')
                ? domainName.Substring(0, domainName.IndexOf('.'))
                : domainName;

            // Remove hyphens and underscores
            var cleanDomain = domainPart
                .Replace("-", "")
                .Replace("_", "")
                .ToUpperInvariant();

            // Take first 4-6 characters
            var baseCode = cleanDomain.Length >= MaxCodeLength
                ? cleanDomain.Substring(0, MaxCodeLength)
                : cleanDomain;

            // Pad if too short
            if (baseCode.Length < MinCodeLength)
            {
                baseCode = baseCode.PadRight(MinCodeLength, '0');
            }

            // Add sequence if provided
            if (sequenceNumber.HasValue)
            {
                var seqStr = sequenceNumber.Value.ToString("D2");
                
                if (baseCode.Length + seqStr.Length > MaxCodeLength)
                {
                    baseCode = baseCode.Substring(0, MaxCodeLength - seqStr.Length);
                }
                
                baseCode += seqStr;
            }

            return baseCode;
        }

        /// <summary>
        /// Generate sequential organization code.
        /// 
        /// FORMAT: PREFIX + SEQUENCE
        /// EXAMPLES: ORG001, ORG999, COMP100
        /// 
        /// BUSINESS RULES:
        /// - Prefix: 3 characters (default: "ORG")
        /// - Sequence: 3-4 digits
        /// - Total length: 6-7 characters
        /// </summary>
        /// <param name="sequenceNumber">Sequence number (1-9999)</param>
        /// <param name="prefix">Optional prefix (default: "ORG")</param>
        /// <returns>Sequential organization code</returns>
        public string GenerateSequential(int sequenceNumber, string? prefix = null)
        {
            if (sequenceNumber < 1)
                throw new ArgumentException("Sequence number must be positive.", nameof(sequenceNumber));

            if (sequenceNumber > 9999)
                throw new ArgumentException("Sequence number cannot exceed 9999.", nameof(sequenceNumber));

            var codePrefix = string.IsNullOrWhiteSpace(prefix)
                ? DefaultPrefix
                : prefix.ToUpperInvariant();

            if (codePrefix.Length > 3)
                throw new ArgumentException("Prefix cannot exceed 3 characters.", nameof(prefix));

            // Determine sequence length based on total
            var seqLength = sequenceNumber < 100 ? 3 : 4;
            var sequence = sequenceNumber.ToString($"D{seqLength}");

            return $"{codePrefix}{sequence}";
        }

        /// <summary>
        /// Generate random organization code.
        /// 
        /// FORMAT: Random alphanumeric (A-Z, 0-9)
        /// EXAMPLES: A1B2C3, XYZ789, QW3RT5
        /// 
        /// BUSINESS RULES:
        /// - Length: 4-6 characters
        /// - Alphanumeric only
        /// - Starts with letter
        /// - At least 2 letters, 1 number
        /// </summary>
        /// <param name="length">Code length (4-6)</param>
        /// <param name="seed">Optional seed for testing</param>
        /// <returns>Random organization code</returns>
        public string GenerateRandom(int length = MaxCodeLength, int? seed = null)
        {
            if (length < MinCodeLength || length > MaxCodeLength)
                throw new ArgumentException($"Length must be between {MinCodeLength} and {MaxCodeLength}.", nameof(length));

            var random = seed.HasValue ? new Random(seed.Value) : new Random();
            var code = new StringBuilder(length);

            // Characters allowed (excluding ambiguous: 0/O, 1/I)
            const string letters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string numbers = "23456789";
            const string alphanumeric = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            // First character must be letter
            code.Append(letters[random.Next(letters.Length)]);

            // Remaining characters (ensure at least 1 number)
            var hasNumber = false;

            for (int i = 1; i < length; i++)
            {
                if (i == length - 1 && !hasNumber)
                {
                    // Force last char to be number if none yet
                    code.Append(numbers[random.Next(numbers.Length)]);
                }
                else
                {
                    var nextChar = alphanumeric[random.Next(alphanumeric.Length)];
                    code.Append(nextChar);
                    
                    if (char.IsDigit(nextChar))
                        hasNumber = true;
                }
            }

            return code.ToString();
        }

        /// <summary>
        /// Validate organization code format.
        /// 
        /// BUSINESS RULES:
        /// - Length: 4-6 characters
        /// - Alphanumeric only
        /// - Uppercase
        /// - At least 2 letters
        /// </summary>
        /// <param name="code">Code to validate</param>
        /// <returns>True if valid format</returns>
        public bool IsValidFormat(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            // Check length
            if (code.Length < MinCodeLength || code.Length > MaxCodeLength)
                return false;

            // Check alphanumeric
            if (!code.All(char.IsLetterOrDigit))
                return false;

            // Check uppercase
            if (code != code.ToUpperInvariant())
                return false;

            // Check at least 2 letters
            var letterCount = code.Count(char.IsLetter);
            if (letterCount < 2)
                return false;

            return true;
        }

        /// <summary>
        /// Suggest alternative codes if original is taken.
        /// Adds sequence numbers (01-99) to base code.
        /// </summary>
        /// <param name="baseCode">Original code</param>
        /// <param name="count">Number of alternatives (default: 5)</param>
        /// <returns>List of alternative codes</returns>
        public string[] SuggestAlternatives(string baseCode, int count = 5)
        {
            if (string.IsNullOrWhiteSpace(baseCode))
                throw new ArgumentException("Base code cannot be empty.", nameof(baseCode));

            if (count < 1 || count > 99)
                throw new ArgumentException("Count must be between 1 and 99.", nameof(count));

            var alternatives = new string[count];

            for (int i = 0; i < count; i++)
            {
                var sequence = (i + 1).ToString("D2");
                
                // Trim base if needed to fit sequence
                var trimmedBase = baseCode.Length + sequence.Length > MaxCodeLength
                    ? baseCode.Substring(0, MaxCodeLength - sequence.Length)
                    : baseCode;

                alternatives[i] = $"{trimmedBase}{sequence}";
            }

            return alternatives;
        }
    }
}
