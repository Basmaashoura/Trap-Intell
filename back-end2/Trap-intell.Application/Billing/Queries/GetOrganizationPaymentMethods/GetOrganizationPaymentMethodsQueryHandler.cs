using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Queries.GetOrganizationPaymentMethods;

internal sealed class GetOrganizationPaymentMethodsQueryHandler : IRequestHandler<GetOrganizationPaymentMethodsQuery, Result<IReadOnlyList<PaymentMethodSummaryDto>>>
{
    private readonly IPaymentMethodRepository _paymentMethodRepository;

    public GetOrganizationPaymentMethodsQueryHandler(IPaymentMethodRepository paymentMethodRepository)
    {
        _paymentMethodRepository = paymentMethodRepository;
    }

    public async Task<Result<IReadOnlyList<PaymentMethodSummaryDto>>> Handle(GetOrganizationPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        if (request.OrganizationId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<PaymentMethodSummaryDto>>(Error.Custom(
                "Organization.InvalidId",
                "Organization ID cannot be empty."));
        }

        IEnumerable<PaymentMethod> methods = request.Status.HasValue
            ? await _paymentMethodRepository.GetByStatusAsync(request.Status.Value, cancellationToken)
            : await _paymentMethodRepository.GetByOrganizationIdAsync(request.OrganizationId, cancellationToken);

        var result = methods
            .Where(method => method.OrganizationId == request.OrganizationId)
            .OrderByDescending(method => method.IsDefault)
            .ThenByDescending(method => method.UpdatedAt)
            .Select(method => new PaymentMethodSummaryDto(
                method.Id,
                method.OrganizationId,
                method.Type,
                method.Status,
                method.IsDefault,
                method.Details.LastFourDigits,
                method.Details.CardBrand,
                method.Details.BillingContactEmail,
                method.Details.ExpiresAt,
                method.IsUsable,
                method.IsExpired,
                method.GetDaysUntilExpiration(),
                method.GetExpirationStatusMessage(),
                method.UpdatedAt))
            .ToList();

        return Result.Success<IReadOnlyList<PaymentMethodSummaryDto>>(result);
    }
}
