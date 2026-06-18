using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Billing.Commands.UpdatePaymentMethod;

public sealed record UpdatePaymentMethodCommand(
    Guid OrganizationId,
    Guid PaymentMethodId,
    string? LastFourDigits,
    string? CardBrand,
    string? PaymentProcessor,
    string? Token,
    DateTime? ExpiresAt,
    string? BillingContactEmail) : IRequest<Result>;

public sealed class UpdatePaymentMethodCommandValidator : AbstractValidator<UpdatePaymentMethodCommand>
{
    public UpdatePaymentMethodCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.PaymentMethodId).NotEmpty();

        RuleFor(x => x.LastFourDigits)
            .Matches("^[0-9]{4}$")
            .When(x => !string.IsNullOrWhiteSpace(x.LastFourDigits))
            .WithMessage("Last four digits must be exactly 4 digits.");

        RuleFor(x => x.BillingContactEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.BillingContactEmail));
    }
}
