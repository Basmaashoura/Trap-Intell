using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Services;

internal interface IPostPaymentSubscriptionRenewalService
{
    Task TryRenewAfterPaidInvoiceAsync(Invoice invoice, CancellationToken cancellationToken);
}
