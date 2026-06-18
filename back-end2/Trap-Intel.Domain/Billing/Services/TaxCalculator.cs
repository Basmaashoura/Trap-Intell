using System;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Billing.Services
{
    /// <summary>
    /// Domain service for tax calculations and breakdowns.
    /// 
    /// SINGLE RESPONSIBILITY: Calculate tax amounts.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (tax calculations)
    /// ? NO repositories or infrastructure dependencies
    /// ? Stateless and deterministic
    /// ? Single responsibility (only tax calculation)
    /// 
    /// Lines: ~160 (SOLID-compliant)
    /// </summary>
    public class TaxCalculator
    {
        private readonly TaxRateProvider _rateProvider;

        public TaxCalculator(TaxRateProvider rateProvider)
        {
            _rateProvider = rateProvider ?? throw new ArgumentNullException(nameof(rateProvider));
        }

        /// <summary>
        /// Calculate tax amount based on jurisdiction.
        /// 
        /// BUSINESS RULES:
        /// - US: State sales tax (varies by state)
        /// - EU: VAT (Value Added Tax)
        /// - B2B transactions may have tax exemptions
        /// </summary>
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

            var taxRate = _rateProvider.GetTaxRate(jurisdiction);
            return amount * taxRate;
        }

        /// <summary>
        /// Calculate VAT (Value Added Tax) for international transactions.
        /// 
        /// BUSINESS RULES:
        /// - VAT is applied in EU countries
        /// - B2B transactions within EU are often reverse-charged (0% VAT)
        /// - B2C transactions always include VAT
        /// </summary>
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
            if (isB2B && _rateProvider.IsEUCountry(countryCode))
                return 0;

            var vatRate = _rateProvider.GetVATRate(countryCode);
            return netAmount * vatRate;
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
        public decimal CalculateReverseVAT(
            decimal grossAmount,
            string countryCode)
        {
            if (grossAmount < 0)
                throw new ArgumentException("Gross amount cannot be negative.", nameof(grossAmount));

            if (string.IsNullOrWhiteSpace(countryCode))
                throw new ArgumentException("Country code cannot be empty.", nameof(countryCode));

            var vatRate = _rateProvider.GetVATRate(countryCode.ToUpperInvariant());
            var netAmount = grossAmount / (1 + vatRate);
            return grossAmount - netAmount;
        }

        /// <summary>
        /// Calculate effective tax rate for a jurisdiction.
        /// Useful for displaying estimated tax in UI.
        /// </summary>
        public decimal GetEffectiveTaxRate(
            string jurisdiction,
            bool isBusiness = false)
        {
            if (string.IsNullOrWhiteSpace(jurisdiction))
                return 0;

            jurisdiction = jurisdiction.ToUpperInvariant();

            if (isBusiness && IsB2BTaxExempt(jurisdiction))
                return 0;

            return _rateProvider.GetTaxRate(jurisdiction);
        }

        #region Private Helpers

        /// <summary>
        /// Determine if B2B transactions are tax-exempt in jurisdiction.
        /// </summary>
        private bool IsB2BTaxExempt(string jurisdiction)
        {
            jurisdiction = jurisdiction.ToUpperInvariant();

            // B2B reverse charge applies in EU
            return _rateProvider.IsEUCountry(jurisdiction);
        }

        #endregion
    }
}
