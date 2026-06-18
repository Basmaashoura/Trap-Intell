using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Shared
{
    /// <summary>
    /// Value object for physical address with validation.
    /// </summary>
    public record Address
    {
        public string Street { get; }
        public string City { get; }
        public string State { get; }
        public string PostalCode { get; }
        public string Country { get; }

        private Address(string street, string city, string state, string postalCode, string country)
        {
            Street = street;
            City = city;
            State = state;
            PostalCode = postalCode;
            Country = country;
        }

        /// <summary>
        /// Factory method to create a validated Address.
        /// </summary>
        public static Result<Address> Create(
            string street,
            string city,
            string state,
            string postalCode,
            string country = "")
        {
            var errors = new List<Error>();

            // Street validation
            if (string.IsNullOrWhiteSpace(street))
                errors.Add(Error.Custom("Address.InvalidStreet", "Street cannot be empty"));
            else if (street.Length < 3)
                errors.Add(Error.Custom("Address.StreetTooShort", "Street must be at least 3 characters"));
            else if (street.Length > 200)
                errors.Add(Error.Custom("Address.StreetTooLong", "Street cannot exceed 200 characters"));

            // City validation
            if (string.IsNullOrWhiteSpace(city))
                errors.Add(Error.Custom("Address.InvalidCity", "City cannot be empty"));
            else if (city.Length < 2)
                errors.Add(Error.Custom("Address.CityTooShort", "City must be at least 2 characters"));
            else if (city.Length > 100)
                errors.Add(Error.Custom("Address.CityTooLong", "City cannot exceed 100 characters"));

            // State validation
            if (string.IsNullOrWhiteSpace(state))
                errors.Add(Error.Custom("Address.InvalidState", "State cannot be empty"));
            else if (state.Length < 2)
                errors.Add(Error.Custom("Address.StateTooShort", "State must be at least 2 characters"));
            else if (state.Length > 50)
                errors.Add(Error.Custom("Address.StateTooLong", "State cannot exceed 50 characters"));

            // Postal code validation
            if (string.IsNullOrWhiteSpace(postalCode))
                errors.Add(Error.Custom("Address.InvalidPostalCode", "Postal code cannot be empty"));
            else if (postalCode.Length < 3)
                errors.Add(Error.Custom("Address.PostalCodeTooShort", "Postal code must be at least 3 characters"));
            else if (postalCode.Length > 20)
                errors.Add(Error.Custom("Address.PostalCodeTooLong", "Postal code cannot exceed 20 characters"));

            // Country validation (optional)
            if (!string.IsNullOrWhiteSpace(country) && country.Length > 100)
                errors.Add(Error.Custom("Address.CountryTooLong", "Country cannot exceed 100 characters"));

            if (errors.Count > 0)
                return Result.Failure<Address>(errors);

            return Result.Success(new Address(
                street.Trim(),
                city.Trim(),
                state.Trim(),
                postalCode.Trim(),
                country?.Trim() ?? string.Empty));
        }
    }

    /// <summary>
    /// Value object for contact information with validation.
    /// </summary>
    public record ContactInfo
    {
        public string Email { get; }
        public string Phone { get; }
        public string? Website { get; }

        private ContactInfo(string email, string phone, string? website)
        {
            Email = email;
            Phone = phone;
            Website = website;
        }

        /// <summary>
        /// Factory method to create validated ContactInfo.
        /// </summary>
        public static Result<ContactInfo> Create(string email, string phone, string? website = null)
        {
            var errors = new List<Error>();

            // Email validation
            if (string.IsNullOrWhiteSpace(email))
                errors.Add(Error.Custom("ContactInfo.EmailRequired", "Email cannot be empty"));
            else if (!IsValidEmail(email))
                errors.Add(Error.Custom("ContactInfo.InvalidEmail", "Email format is invalid"));
            else if (email.Length > 254)
                errors.Add(Error.Custom("ContactInfo.EmailTooLong", "Email cannot exceed 254 characters"));

            // Phone validation
            if (string.IsNullOrWhiteSpace(phone))
                errors.Add(Error.Custom("ContactInfo.PhoneRequired", "Phone cannot be empty"));
            else if (!IsValidPhone(phone))
                errors.Add(Error.Custom("ContactInfo.InvalidPhone", "Phone must contain 10-15 digits"));

            // Website validation (optional)
            if (!string.IsNullOrWhiteSpace(website))
            {
                if (!IsValidUrl(website))
                    errors.Add(Error.Custom("ContactInfo.InvalidWebsite", "Website URL format is invalid"));
                else if (website.Length > 500)
                    errors.Add(Error.Custom("ContactInfo.WebsiteTooLong", "Website URL cannot exceed 500 characters"));
            }

            if (errors.Count > 0)
                return Result.Failure<ContactInfo>(errors);

            return Result.Success(new ContactInfo(
                email.Trim().ToLowerInvariant(),
                phone.Trim(),
                website?.Trim()));
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidPhone(string phone)
        {
            // Remove all non-digit characters for validation
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            return digits.Length >= 10 && digits.Length <= 15;
        }

        private static bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }

    /// <summary>
    /// Value object for tax identification with validation.
    /// </summary>
    public record TaxIdentifier
    {
        public string TaxId { get; }

        private TaxIdentifier(string taxId)
        {
            TaxId = taxId;
        }

        /// <summary>
        /// Factory method to create validated TaxIdentifier.
        /// </summary>
        public static Result<TaxIdentifier> Create(string taxId)
        {
            if (string.IsNullOrWhiteSpace(taxId))
                return Result.Failure<TaxIdentifier>(
                    Error.Custom("TaxIdentifier.Required", "Tax ID cannot be empty"));

            // Clean the tax ID (remove common separators)
            var cleaned = taxId.Trim()
                .Replace("-", "")
                .Replace(" ", "")
                .Replace(".", "")
                .ToUpperInvariant();

            if (cleaned.Length < 5)
                return Result.Failure<TaxIdentifier>(
                    Error.Custom("TaxIdentifier.TooShort", "Tax ID must be at least 5 characters"));

            if (cleaned.Length > 50)
                return Result.Failure<TaxIdentifier>(
                    Error.Custom("TaxIdentifier.TooLong", "Tax ID cannot exceed 50 characters"));

            // Validate alphanumeric
            if (!cleaned.All(c => char.IsLetterOrDigit(c)))
                return Result.Failure<TaxIdentifier>(
                    Error.Custom("TaxIdentifier.InvalidFormat", "Tax ID must contain only letters and numbers"));

            return Result.Success(new TaxIdentifier(cleaned));
        }
    }

    /// <summary>
    /// Value object for organization domain with validation.
    /// </summary>
    public record OrganizationDomain
    {
        public string Domain { get; }

        private OrganizationDomain(string domain)
        {
            Domain = domain;
        }

        /// <summary>
        /// Factory method to create validated OrganizationDomain.
        /// </summary>
        public static Result<OrganizationDomain> Create(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return Result.Failure<OrganizationDomain>(
                    Error.Custom("OrganizationDomain.Required", "Domain cannot be empty"));

            var cleaned = domain.Trim().ToLowerInvariant();

            if (cleaned.Length < 4)
                return Result.Failure<OrganizationDomain>(
                    Error.Custom("OrganizationDomain.TooShort", "Domain must be at least 4 characters (e.g., a.co)"));

            if (cleaned.Length > 253)
                return Result.Failure<OrganizationDomain>(
                    Error.Custom("OrganizationDomain.TooLong", "Domain cannot exceed 253 characters"));

            if (!IsValidDomainFormat(cleaned))
                return Result.Failure<OrganizationDomain>(
                    Error.Custom("OrganizationDomain.InvalidFormat",
                        "Domain format is invalid. Expected format: example.com"));

            return Result.Success(new OrganizationDomain(cleaned));
        }

        private static bool IsValidDomainFormat(string domain)
        {
            // RFC 1035 compliant domain validation
            // Pattern: subdomain.domain.tld (letters, numbers, hyphens)
            var domainRegex = new Regex(
                @"^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9][a-z0-9-]{0,61}[a-z0-9]$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            return domainRegex.IsMatch(domain);
        }
    }
}
