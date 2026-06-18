using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Queries.GetOrganizationPaymentMethods;

public sealed record PaymentMethodSummaryDto(
    Guid Id,
    Guid OrganizationId,
    PaymentMethodType Type,
    PaymentMethodStatus Status,
    bool IsDefault,
    string? LastFourDigits,
    string? CardBrand,
    string? BillingContactEmail,
    DateTime? ExpiresAt,
    bool IsUsable,
    bool IsExpired,
    int DaysUntilExpiration,
    string ExpirationStatusMessage,
    DateTime UpdatedAt);
