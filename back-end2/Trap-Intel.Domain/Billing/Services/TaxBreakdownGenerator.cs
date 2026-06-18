using System;
using System.Collections.Generic;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Billing.Services
{
    /// <summary>
    /// Domain service for comprehensive tax breakdown generation.
    /// 
    /// SINGLE RESPONSIBILITY: Generate detailed tax breakdowns.
    /// 
    /// This is a TRUE domain service because:
    /// ? Contains pure business logic (tax breakdown)
    /// ? NO repositories or infrastructure dependencies
    /// ? Stateless and deterministic
    /// ? Single responsibility (only tax breakdown)
    /// 
    /// Lines: ~150 (SOLID-compliant)
    /// </summary>
    public class TaxBreakdownGenerator
    {
        private readonly TaxRateProvider _rateProvider;
        private readonly TaxCalculator _taxCalculator;

        public TaxBreakdownGenerator(
            TaxRateProvider rateProvider,
            TaxCalculator taxCalculator)
        {
            _rateProvider = rateProvider ?? throw new ArgumentNullException(nameof(rateProvider));
            _taxCalculator = taxCalculator ?? throw new ArgumentNullException(nameof(taxCalculator));
        }

        /// <summary>
        /// Calculate comprehensive tax breakdown with federal, state, and local components.
        /// 
        /// BUSINESS RULES:
        /// - Federal tax (if applicable)
        /// - State/Provincial tax
        /// - Local/Municipal tax (city, county)
        /// - Total = sum of all components
        /// </summary>
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
                stateTax = _taxCalculator.CalculateTax(amount, billingAddress.State, isBusiness);

                // Local tax (simplified: 1-3% in major cities)
                localTax = CalculateLocalTax(amount, billingAddress.City);
            }
            else
            {
                // International: VAT (acts as federal tax)
                federalTax = _taxCalculator.CalculateVAT(amount, billingAddress.Country, isBusiness);
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
        /// </summary>
        public decimal CalculateOverageTax(
            decimal overageAmount,
            string jurisdiction,
            bool isBusiness = false)
        {
            return _taxCalculator.CalculateTax(overageAmount, jurisdiction, isBusiness);
        }

        /// <summary>
        /// Determine if transaction qualifies for tax exemption.
        /// 
        /// BUSINESS RULES:
        /// - Non-profit organizations (with valid tax-exempt ID)
        /// - Government entities
        /// - Educational institutions
        /// </summary>
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

        #region Private Helpers

        /// <summary>
        /// Calculate local tax (city/county) for specific cities.
        /// </summary>
        private decimal CalculateLocalTax(decimal amount, string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return 0;

            var localRate = _rateProvider.GetLocalTaxRate(city);
            return amount * localRate;
        }

        #endregion
    }
}
