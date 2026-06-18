using System;
using System.Collections.Generic;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Billing.Services
{
    /// <summary>
    /// Domain service for tax calculations across jurisdictions.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (tax calculations)
    /// ? NO repositories or infrastructure dependencies
    /// ? Works with domain value objects only
    /// ? Encapsulates domain knowledge about tax rules
    /// 
    /// BEST PRACTICES FOLLOWED:
    /// - Stateless (no instance state)
    /// - Pure functions (deterministic calculations)
    /// - Single Responsibility (only tax calculations)
    /// - Domain-driven (uses Address, TaxInfo from domain)
    /// 
    /// EVIDENCE FROM CODE ANALYSIS:
    /// In TaxInfo value object:
    ///   public record TaxInfo(string? TaxId, decimal TaxRate);
    ///   // ?? Just stores tax rate. No calculation logic!
    /// 
    /// This service provides comprehensive tax calculation logic.
    /// </summary>
    public class TaxCalculationService
    {
        // Tax rate constants by jurisdiction (simplified for domain service)
        // In production, these would come from a tax rate provider service in infrastructure layer
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
        /// Calculate tax amount based on jurisdiction.
        /// 
        /// BUSINESS RULES:
        /// - US: State sales tax (varies by state)
        /// - EU: VAT (Value Added Tax)
        /// - B2B transactions may have tax exemptions
        /// - Digital services may have special rates
        /// 
        /// EXAMPLE:
        /// - Amount: $1000
        /// - State: California (7.25%)
        /// - Tax: $72.50
        /// </summary>
        /// <param name="amount">Base amount to calculate tax on</param>
        /// <param name="jurisdiction">Tax jurisdiction (state/country code)</param>
        /// <param name="isBusiness">Is this a business-to-business transaction?</param>
        /// <returns>Tax amount</returns>
        public decimal CalculateTax(
            decimal amount,
            string jurisdiction,
            bool isBusiness = false)
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative.", nameof(amount));

            if (string.IsNullOrWhiteSpace(jurisdiction))
                throw new ArgumentException("Jurisdiction cannot be empty.", nameof(jurisdiction));

            jurisdiction = jurisdiction.ToUpperInvariant();

            // B2B tax exemption for certain jurisdictions
            if (isBusiness && IsB2BTaxExempt(jurisdiction))
                return 0;

            var taxRate = GetTaxRate(jurisdiction);
            return amount * taxRate;
        }

        /// <summary>
        /// Calculate VAT (Value Added Tax) for international transactions.
        /// 
        /// BUSINESS RULES:
        /// - VAT is applied in EU countries
        /// - Standard rate varies by country (19-25%)
        /// - B2B transactions within EU are often reverse-charged (0% VAT)
        /// - B2C transactions always include VAT
        /// 
        /// EXAMPLE:
        /// - Net amount: €1000
        /// - Country: Germany (19% VAT)
        /// - VAT: €190
        /// - Gross: €1190
        /// </summary>
        /// <param name="netAmount">Amount before VAT</param>
        /// <param name="countryCode">ISO country code</param>
        /// <param name="isB2B">Is this B2B transaction?</param>
        /// <returns>VAT amount</returns>
        public decimal CalculateVAT(
            decimal netAmount,
            string countryCode,
            bool isB2B = false)
        {
            if (netAmount < 0)
                throw new ArgumentException("Net amount cannot be negative.", nameof(netAmount));

            if (string.IsNullOrWhiteSpace(countryCode))
                throw new ArgumentException("Country code cannot be empty.", nameof(countryCode));

            countryCode = countryCode.ToUpperInvariant();

            // B2B reverse charge: buyer pays VAT in their country
            if (isB2B && CountryVATRates.ContainsKey(countryCode))
                return 0;

            var vatRate = GetVATRate(countryCode);
            return netAmount * vatRate;
        }

        /// <summary>
        /// Calculate comprehensive tax breakdown with federal, state, and local components.
        /// 
        /// BUSINESS RULES:
        /// - Federal tax (if applicable)
        /// - State/Provincial tax
        /// - Local/Municipal tax (city, county)
        /// - Total = sum of all components
        /// 
        /// EXAMPLE:
        /// - Amount: $1000
        /// - Federal: $0 (digital services exempt)
        /// - State (CA): $72.50
        /// - Local (SF): $25.00
        /// - Total: $97.50
        /// </summary>
        /// <param name="amount">Base amount</param>
        /// <param name="billingAddress">Billing address for jurisdiction determination</param>
        /// <param name="isBusiness">Business flag</param>
        /// <returns>Comprehensive tax breakdown</returns>
        public TaxBreakdown CalculateComprehensiveTax(
            decimal amount,
            Address billingAddress,
            bool isBusiness = false)
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative.", nameof(amount));

            if (billingAddress == null)
                throw new ArgumentNullException(nameof(billingAddress));

            // Determine jurisdiction type
            var isUSAddress = billingAddress.Country.Equals("USA", StringComparison.OrdinalIgnoreCase) ||
                             billingAddress.Country.Equals("US", StringComparison.OrdinalIgnoreCase);

            decimal federalTax = 0;
            decimal stateTax = 0;
            decimal localTax = 0;

            if (isUSAddress)
            {
                // US: No federal sales tax for digital services
                federalTax = 0;

                // State tax
                stateTax = CalculateTax(amount, billingAddress.State, isBusiness);

                // Local tax (simplified: 1-3% in major cities)
                localTax = CalculateLocalTax(amount, billingAddress.City);
            }
            else
            {
                // International: VAT (acts as federal tax)
                federalTax = CalculateVAT(amount, billingAddress.Country, isBusiness);
                stateTax = 0;
                localTax = 0;
            }

            var totalTax = federalTax + stateTax + localTax;

            return new TaxBreakdown(
                FederalTax: federalTax,
                StateTax: stateTax,
                LocalTax: localTax,
                TotalTax: totalTax,
                EffectiveRate: amount > 0 ? totalTax / amount : 0);
        }

        /// <summary>
        /// Calculate tax on overage charges.
        /// 
        /// BUSINESS RULE:
        /// - Overage charges are taxed at same rate as base subscription
        /// - Apply same jurisdiction rules
        /// </summary>
        /// <param name="overageAmount">Overage amount</param>
        /// <param name="jurisdiction">Tax jurisdiction</param>
        /// <param name="isBusiness">Business flag</param>
        /// <returns>Tax on overage</returns>
        public decimal CalculateOverageTax(
            decimal overageAmount,
            string jurisdiction,
            bool isBusiness = false)
        {
            return CalculateTax(overageAmount, jurisdiction, isBusiness);
        }

        /// <summary>
        /// Determine if transaction qualifies for tax exemption.
        /// 
        /// BUSINESS RULES:
        /// - Non-profit organizations (with valid tax-exempt ID)
        /// - Government entities
        /// - Educational institutions
        /// - Certain digital services in specific jurisdictions
        /// </summary>
        /// <param name="organizationType">Type of organization</param>
        /// <param name="taxExemptId">Tax exemption ID (if any)</param>
        /// <param name="jurisdiction">Tax jurisdiction</param>
        /// <returns>True if exempt from tax</returns>
        public bool IsTaxExempt(
            Organizations.OrganizationType organizationType,
            string? taxExemptId,
            string jurisdiction)
        {
            if (string.IsNullOrWhiteSpace(jurisdiction))
                return false;

            // Organizations with valid tax-exempt ID
            if (!string.IsNullOrWhiteSpace(taxExemptId))
            {
                return organizationType == Organizations.OrganizationType.NGO ||
                       organizationType == Organizations.OrganizationType.Educational ||
                       organizationType == Organizations.OrganizationType.Government;
            }

            return false;
        }

        /// <summary>
        /// Calculate reverse VAT from gross amount (extract VAT included in price).
        /// 
        /// BUSINESS RULE:
        /// - Net = Gross / (1 + VAT Rate)
        /// - VAT = Gross - Net
        /// 
        /// EXAMPLE:
        /// - Gross: €1190 (includes 19% VAT)
        /// - Net: €1190 / 1.19 = €1000
        /// - VAT: €190
        /// </summary>
        /// <param name="grossAmount">Amount including VAT</param>
        /// <param name="countryCode">Country code</param>
        /// <returns>VAT amount extracted from gross</returns>
        public decimal CalculateReverseVAT(
            decimal grossAmount,
            string countryCode)
        {
            if (grossAmount < 0)
                throw new ArgumentException("Gross amount cannot be negative.", nameof(grossAmount));

            if (string.IsNullOrWhiteSpace(countryCode))
                throw new ArgumentException("Country code cannot be empty.", nameof(countryCode));

            var vatRate = GetVATRate(countryCode.ToUpperInvariant());
            var netAmount = grossAmount / (1 + vatRate);
            return grossAmount - netAmount;
        }

        /// <summary>
        /// Get effective tax rate for a jurisdiction.
        /// Useful for displaying estimated tax in UI.
        /// </summary>
        /// <param name="jurisdiction">Jurisdiction code</param>
        /// <param name="isBusiness">Business flag</param>
        /// <returns>Effective tax rate (0-1)</returns>
        public decimal GetEffectiveTaxRate(
            string jurisdiction,
            bool isBusiness = false)
        {
            if (string.IsNullOrWhiteSpace(jurisdiction))
                return 0;

            jurisdiction = jurisdiction.ToUpperInvariant();

            if (isBusiness && IsB2BTaxExempt(jurisdiction))
                return 0;

            return GetTaxRate(jurisdiction);
        }

        #region Private Helper Methods

        /// <summary>
        /// Get tax rate for a jurisdiction.
        /// Checks US states first, then international VAT rates.
        /// </summary>
        private decimal GetTaxRate(string jurisdiction)
        {
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
        private decimal GetVATRate(string countryCode)
        {
            countryCode = countryCode.ToUpperInvariant();

            if (CountryVATRates.TryGetValue(countryCode, out var vatRate))
                return vatRate;

            // Default: no VAT
            return 0;
        }

        /// <summary>
        /// Calculate local tax (city/county) for specific cities.
        /// Simplified implementation - in production, would need detailed tax tables.
        /// </summary>
        private decimal CalculateLocalTax(decimal amount, string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return 0;

            // Simplified local tax rates for major US cities
            var localRate = city.ToUpperInvariant() switch
            {
                var c when c.Contains("SAN FRANCISCO") => 0.025m,  // 2.5%
                var c when c.Contains("NEW YORK") => 0.045m,       // 4.5%
                var c when c.Contains("CHICAGO") => 0.025m,        // 2.5%
                var c when c.Contains("LOS ANGELES") => 0.01m,     // 1%
                var c when c.Contains("SEATTLE") => 0.035m,        // 3.5%
                _ => 0.01m // Default 1% for other cities
            };

            return amount * localRate;
        }

        /// <summary>
        /// Determine if B2B transactions are tax-exempt in jurisdiction.
        /// </summary>
        private bool IsB2BTaxExempt(string jurisdiction)
        {
            jurisdiction = jurisdiction.ToUpperInvariant();

            // B2B reverse charge applies in EU
            return CountryVATRates.ContainsKey(jurisdiction);
        }

        #endregion
    }

    #region Supporting Value Objects

    /// <summary>
    /// Value object representing comprehensive tax breakdown.
    /// </summary>
    public record TaxBreakdown(
        decimal FederalTax,
        decimal StateTax,
        decimal LocalTax,
        decimal TotalTax,
        decimal EffectiveRate)
    {
        /// <summary>
        /// Get formatted summary of tax breakdown.
        /// </summary>
        public string GetSummary()
        {
            var parts = new List<string>();

            if (FederalTax > 0)
                parts.Add($"Federal: ${FederalTax:F2}");

            if (StateTax > 0)
                parts.Add($"State: ${StateTax:F2}");

            if (LocalTax > 0)
                parts.Add($"Local: ${LocalTax:F2}");

            return $"{string.Join(", ", parts)} | Total: ${TotalTax:F2} ({EffectiveRate:P2})";
        }

        /// <summary>
        /// Check if any tax applies.
        /// </summary>
        public bool HasTax => TotalTax > 0;
    }

    #endregion
}
