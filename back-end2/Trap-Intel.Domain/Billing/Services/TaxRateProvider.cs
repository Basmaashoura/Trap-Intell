using System;
using System.Collections.Generic;

namespace Trap_Intel.Domain.Billing.Services
{
    /// <summary>
    /// Domain service for managing tax rates by jurisdiction.
    /// 
    /// SINGLE RESPONSIBILITY: Tax rate lookup and management.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (tax rates)
    /// ? NO repositories or infrastructure dependencies
    /// ? Stateless (rates are constants)
    /// ? Single responsibility (only rate lookup)
    /// 
    /// Lines: ~140 (SOLID-compliant)
    /// </summary>
    public class TaxRateProvider
    {
        // US state tax rates
        private static readonly Dictionary<string, decimal> USStateTaxRates = new()
        {
            { "CA", 0.0725m },  // California: 7.25%
            { "NY", 0.08m },    // New York: 8%
            { "TX", 0.0625m },  // Texas: 6.25%
            { "FL", 0.06m },    // Florida: 6%
            { "WA", 0.065m },   // Washington: 6.5%
            { "IL", 0.0625m },  // Illinois: 6.25%
            { "PA", 0.06m },    // Pennsylvania: 6%
            { "OH", 0.0575m },  // Ohio: 5.75%
            { "GA", 0.04m },    // Georgia: 4%
            { "NC", 0.0475m },  // North Carolina: 4.75%
        };

        // Country VAT rates
        private static readonly Dictionary<string, decimal> CountryVATRates = new()
        {
            { "GB", 0.20m },    // UK: 20%
            { "DE", 0.19m },    // Germany: 19%
            { "FR", 0.20m },    // France: 20%
            { "IT", 0.22m },    // Italy: 22%
            { "ES", 0.21m },    // Spain: 21%
            { "NL", 0.21m },    // Netherlands: 21%
            { "SE", 0.25m },    // Sweden: 25%
            { "PL", 0.23m },    // Poland: 23%
            { "BE", 0.21m },    // Belgium: 21%
            { "AT", 0.20m },    // Austria: 20%
        };

        /// <summary>
        /// Get tax rate for a jurisdiction.
        /// Checks US states first, then international VAT rates.
        /// </summary>
        public decimal GetTaxRate(string jurisdiction)
        {
            if (string.IsNullOrWhiteSpace(jurisdiction))
                return 0;

            jurisdiction = jurisdiction.ToUpperInvariant();

            // Check US states
            if (USStateTaxRates.TryGetValue(jurisdiction, out var stateTaxRate))
                return stateTaxRate;

            // Check country VAT
            if (CountryVATRates.TryGetValue(jurisdiction, out var vatRate))
                return vatRate;

            // Default: no tax (could be logged for missing jurisdictions)
            return 0;
        }

        /// <summary>
        /// Get VAT rate for a country.
        /// </summary>
        public decimal GetVATRate(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return 0;

            countryCode = countryCode.ToUpperInvariant();

            if (CountryVATRates.TryGetValue(countryCode, out var vatRate))
                return vatRate;

            return 0;
        }

        /// <summary>
        /// Check if jurisdiction has tax.
        /// </summary>
        public bool HasTax(string jurisdiction)
        {
            return GetTaxRate(jurisdiction) > 0;
        }

        /// <summary>
        /// Check if jurisdiction is in EU (has VAT).
        /// </summary>
        public bool IsEUCountry(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return false;

            return CountryVATRates.ContainsKey(countryCode.ToUpperInvariant());
        }

        /// <summary>
        /// Get local tax rate for specific cities.
        /// Simplified implementation - in production, would need detailed tax tables.
        /// </summary>
        public decimal GetLocalTaxRate(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return 0;

            var localRate = city.ToUpperInvariant() switch
            {
                var c when c.Contains("SAN FRANCISCO") => 0.025m,  // 2.5%
                var c when c.Contains("NEW YORK") => 0.045m,       // 4.5%
                var c when c.Contains("CHICAGO") => 0.025m,        // 2.5%
                var c when c.Contains("LOS ANGELES") => 0.01m,     // 1%
                var c when c.Contains("SEATTLE") => 0.035m,        // 3.5%
                _ => 0.01m // Default 1% for other cities
            };

            return localRate;
        }
    }
}
