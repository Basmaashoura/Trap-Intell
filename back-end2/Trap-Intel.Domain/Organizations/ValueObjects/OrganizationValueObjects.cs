using System;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Organizations
{
    /// <summary>
    /// Value object for organization settings/configuration.
    /// </summary>
    public record OrganizationSettings(
        bool AllowMultipleAddresses = true,
        bool RequireApprovalForMembers = false,
        int MaximumMembers = 1000,
        bool EnableBilling = true,
        bool EnableApiAccess = false);

    /// <summary>
    /// Value object for organization subscription.
    /// </summary>
    public record Subscription(
        string PlanName,
        DateTime StartDate,
        DateTime? EndDate = null,
        bool IsActive = true);

    /// <summary>
    /// Value object for billing address.
    /// </summary>
    public record BillingInfo(
        Address Address,
        string BillingEmail,
        string PaymentMethod = "Credit Card");
}
